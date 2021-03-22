#nullable enable
namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using Antlr4.Runtime;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.CodeGen;
    using ScTools.ScriptAssembly.Grammar;

    public class Assembler : IDisposable
    {
        public const string DefaultScriptName = "unknown";

        public enum Segment { None, Global, Static, Arg, String, Code, Include }

        public static int GetAddressingUnitByteSize(Segment segment) => segment switch
        {
            // ScriptValue-addressable
            Segment.Global or Segment.Static or Segment.Arg => Marshal.SizeOf<ScriptValue>(),

            // NativeHash-addressable
            Segment.Include => sizeof(ulong),

            // byte-addressable
            _ => sizeof(byte),
        };


        private bool disposed;

        private readonly SegmentBuilder globalSegmentBuilder = new(GetAddressingUnitByteSize(Segment.Global), isPaged: true),
                                        staticSegmentBuilder = new(GetAddressingUnitByteSize(Segment.Static), isPaged: false),
                                        argSegmentBuilder = new(GetAddressingUnitByteSize(Segment.Arg), isPaged: false), // appended to the end of the static segment
                                        stringSegmentBuilder = new(GetAddressingUnitByteSize(Segment.String), isPaged: true),
                                        codeSegmentBuilder = new(GetAddressingUnitByteSize(Segment.Code), isPaged: true),
                                        includeSegmentBuilder = new(GetAddressingUnitByteSize(Segment.Include), isPaged: false);
        private readonly CodeBuilder codeBuilder;

        private Segment CurrentSegment { get; set; } = Segment.None;
        private SegmentBuilder CurrentSegmentBuilder => CurrentSegment switch
        {
            Segment.Global => globalSegmentBuilder,
            Segment.Static => staticSegmentBuilder,
            Segment.Arg => argSegmentBuilder,
            Segment.String => stringSegmentBuilder,
            Segment.Code => codeSegmentBuilder,
            Segment.Include => includeSegmentBuilder,
            _ => throw new InvalidOperationException(), 
        };

        public DiagnosticsReport Diagnostics { get; }
        public Script OutputScript { get; }
        public Dictionary<string, ConstantValue> Constants { get; }
        public Dictionary<string, Label> Labels { get; }
        public bool HasScriptName { get; private set; }
        public bool HasScriptHash { get; private set; }
        public bool HasGlobalBlock { get; private set; }

        public Assembler(string filePath)
        {
            Diagnostics = new(filePath);
            OutputScript = new()
            {
                Name = DefaultScriptName,
                NameHash = DefaultScriptName.ToLowercaseHash(),
            };
            Constants = new(CaseInsensitiveComparer);
            Labels = new(CaseInsensitiveComparer);
            codeBuilder = new CodeBuilder(codeSegmentBuilder);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    globalSegmentBuilder.Dispose();
                    staticSegmentBuilder.Dispose();
                    argSegmentBuilder.Dispose();
                    stringSegmentBuilder.Dispose();
                    codeSegmentBuilder.Dispose();
                    includeSegmentBuilder.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Assemble(TextReader input)
        {
            var inputStream = new AntlrInputStream(input);

            var lexer = new ScAsmLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new SyntaxErrorListener<int>(this));
            var tokens = new CommonTokenStream(lexer);
            var parser = new ScAsmParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new SyntaxErrorListener<IToken>(this));

            var program = parser.program();
            foreach (var line in program.line())
            {
                ProcessLine(line);
            }
            FixArgLabels();
            codeBuilder.ResolveLabelTargets(Labels, Constants, Diagnostics);

            OutputScript.CodePages = codeSegmentBuilder.ToPages<byte>();
            OutputScript.CodeLength = OutputScript.CodePages.Length;

            OutputScript.GlobalsPages = globalSegmentBuilder.ToPages<ScriptValue>();
            OutputScript.GlobalsLength = OutputScript.GlobalsPages.Length;

            (OutputScript.Statics, OutputScript.StaticsCount, OutputScript.ArgsCount) = SegmentToStaticsArray(staticSegmentBuilder, argSegmentBuilder);
            (OutputScript.Natives, OutputScript.NativesCount) = SegmentToNativesArray(includeSegmentBuilder, OutputScript.CodeLength);

            OutputScript.StringsPages = stringSegmentBuilder.ToPages<byte>();
            OutputScript.StringsLength = OutputScript.StringsPages.Length;
        }

        private void ProcessLine(ScAsmParser.LineContext line)
        {
            var label = line.label();
            var segmentDirective = line.segmentDirective();
            var directive = line.directive();
            var instruction = line.instruction();

            if (label is not null)
            {
                ProcessLabel(label);
            }

            if (segmentDirective is not null)
            {
                ProcessSegmentDirective(segmentDirective);
            }
            else if (directive is not null)
            {
                ProcessDirective(directive);
            }
            else if (instruction is not null)
            {
                ProcessInstruction(instruction);
            }
        }

        private void ProcessLabel(ScAsmParser.LabelContext label)
        {
            if (CurrentSegment == Segment.None)
            {
                Diagnostics.AddError($"Unexpected label outside any segment", Source(label));
                return;
            }

            var name = label.identifier().GetText();
            var offset = CurrentSegmentBuilder.ByteLength / GetAddressingUnitByteSize(CurrentSegment);

            if (CurrentSegment == Segment.Global)
            {
                offset |= (int)(OutputScript.GlobalsBlock << 18);
            }

            if (!Labels.TryAdd(name, new Label(CurrentSegment, offset)))
            {
                Diagnostics.AddError($"Label '{name}' already defined", Source(label));
            }

            if (CurrentSegment == Segment.Code)
            {
                codeBuilder.Label = name;
            }
        }

        private void ProcessSegmentDirective(ScAsmParser.SegmentDirectiveContext directive)
        {
            CurrentSegment = directive switch
            {
                ScAsmParser.GlobalSegmentDirectiveContext => Segment.Global,
                ScAsmParser.StaticSegmentDirectiveContext => Segment.Static,
                ScAsmParser.ArgSegmentDirectiveContext => Segment.Arg,
                ScAsmParser.StringSegmentDirectiveContext => Segment.String,
                ScAsmParser.CodeSegmentDirectiveContext => Segment.Code,
                ScAsmParser.IncludeSegmentDirectiveContext => Segment.Include,
                _ => throw new InvalidOperationException(),
            };

            if (CurrentSegment == Segment.Global && !HasGlobalBlock)
            {
                Diagnostics.AddError($"Directive '.global_block' required before '.global' segment", Source(directive));
            }
        }

        private void ProcessDirective(ScAsmParser.DirectiveContext directive)
        {
            switch (directive)
            {
                case ScAsmParser.ScriptNameDirectiveContext nameDirective:
                    if (HasScriptName)
                    {
                        Diagnostics.AddError($"Directive '.script_name' is repeated", Source(nameDirective));
                    }
                    else
                    {
                        OutputScript.Name = nameDirective.identifier().GetText();
                        OutputScript.NameHash = OutputScript.Name.ToLowercaseHash();
                        HasScriptName = true;
                    }
                    break;
                case ScAsmParser.ScriptHashDirectiveContext hashDirective:
                    if (HasScriptHash)
                    {
                        Diagnostics.AddError($"Directive '.script_hash' is repeated", Source(hashDirective));
                    }
                    else
                    {
                        OutputScript.Hash = (uint)hashDirective.integer().GetText().ParseAsInt();
                        HasScriptHash = true;
                    }
                    break;
                case ScAsmParser.GlobalBlockDirectiveContext globalBlockDirective:
                    if (HasGlobalBlock)
                    {
                        Diagnostics.AddError($"Directive '.global_block' is repeated", Source(globalBlockDirective));
                    }
                    else
                    {
                        OutputScript.GlobalsBlock = (uint)globalBlockDirective.integer().GetText().ParseAsInt();
                        HasGlobalBlock = true;
                    }
                    break;
                case ScAsmParser.ConstDirectiveContext constDirective:
                    var constName = constDirective.identifier();
                    var constInteger = constDirective.integer();
                    var constFloat = constDirective.@float();

                    var constValue = constInteger != null ?
                                        new ConstantValue(constInteger.GetText().ParseAsInt()) :
                                        new ConstantValue(constFloat.GetText().ParseAsFloat());

                    if (!Constants.TryAdd(constName.GetText(), constValue))
                    {
                        Diagnostics.AddError($"Constant '{constName}' already defined", Source(constName));
                    }
                    break;
                case ScAsmParser.IntDirectiveContext intDirective:
                    WriteIntFloatDirectiveOperands(intDirective.directiveOperandList(), isFloat: false);
                    break;
                case ScAsmParser.FloatDirectiveContext floatDirective:
                    WriteIntFloatDirectiveOperands(floatDirective.directiveOperandList(), isFloat: true);
                    break;
                case ScAsmParser.StrDirectiveContext strDirective:
                    CurrentSegmentBuilder.String(strDirective.@string().GetText()[1..^1].Unescape());
                    break;
                case ScAsmParser.NativeDirectiveContext nativeDirective:
                    CurrentSegmentBuilder.UInt64(nativeDirective.integer().GetText().ParseAsUInt64());
                    break;
            }
        }

        private void ProcessInstruction(ScAsmParser.InstructionContext instruction)
        {
            if (CurrentSegment != Segment.Code)
            {
                Diagnostics.AddError($"Unexpected instruction in non-code segment", Source(instruction));
                return;
            }

            if (!Enum.TryParse<Opcode>(instruction.opcode().GetText(), out var opcode))
            {
                throw new InvalidOperationException($"Unknown opcode '{instruction.opcode().GetText()}'");
            }

            var errorOccurred = false;
            var expectedNumOperands = opcode.GetNumberOfOperands();
            var operands = instruction.operandList()?.operand() ?? Array.Empty<ScAsmParser.OperandContext>();
            if (expectedNumOperands != -1 && operands.Length != expectedNumOperands)
            {
                Diagnostics.AddError($"Expected {expectedNumOperands} operands for opcode {opcode} but found {operands.Length} operands", Source(instruction));
                errorOccurred = true;
            }

            codeBuilder.Opcode(opcode);
            switch (opcode)
            {
                case Opcode.PUSH_CONST_U8:
                case Opcode.ARRAY_U8:
                case Opcode.ARRAY_U8_LOAD:
                case Opcode.ARRAY_U8_STORE:
                case Opcode.LOCAL_U8:
                case Opcode.LOCAL_U8_LOAD:
                case Opcode.LOCAL_U8_STORE:
                case Opcode.STATIC_U8:
                case Opcode.STATIC_U8_LOAD:
                case Opcode.STATIC_U8_STORE:
                case Opcode.IADD_U8:
                case Opcode.IMUL_U8:
                case Opcode.IOFFSET_U8:
                case Opcode.IOFFSET_U8_LOAD:
                case Opcode.IOFFSET_U8_STORE:
                case Opcode.TEXT_LABEL_ASSIGN_STRING:
                case Opcode.TEXT_LABEL_ASSIGN_INT:
                case Opcode.TEXT_LABEL_APPEND_STRING:
                case Opcode.TEXT_LABEL_APPEND_INT:
                    OperandToU8(operands[0], ref errorOccurred);
                    break;
                case Opcode.PUSH_CONST_U8_U8:
                case Opcode.LEAVE:
                    OperandToU8(operands[0], ref errorOccurred);
                    OperandToU8(operands[1], ref errorOccurred);
                    break;
                case Opcode.PUSH_CONST_U8_U8_U8:
                    OperandToU8(operands[0], ref errorOccurred);
                    OperandToU8(operands[1], ref errorOccurred);
                    OperandToU8(operands[2], ref errorOccurred);
                    break;
                case Opcode.PUSH_CONST_U32:
                    OperandToU32(operands[0], ref errorOccurred);
                    break;
                case Opcode.PUSH_CONST_F: // TODO
                    break;
                case Opcode.NATIVE: // TODO
                    break;
                case Opcode.ENTER:
                    OperandToU8(operands[0], ref errorOccurred);
                    OperandToU16(operands[1], ref errorOccurred);
                    codeBuilder.U8(0); // TODO: include label name here
                    break;
                case Opcode.PUSH_CONST_S16:
                case Opcode.IADD_S16:
                case Opcode.IMUL_S16:
                case Opcode.IOFFSET_S16:
                case Opcode.IOFFSET_S16_LOAD:
                case Opcode.IOFFSET_S16_STORE: // TODO
                    break;
                case Opcode.ARRAY_U16:
                case Opcode.ARRAY_U16_LOAD:
                case Opcode.ARRAY_U16_STORE:
                case Opcode.LOCAL_U16:
                case Opcode.LOCAL_U16_LOAD:
                case Opcode.LOCAL_U16_STORE:
                case Opcode.STATIC_U16:
                case Opcode.STATIC_U16_LOAD:
                case Opcode.STATIC_U16_STORE:
                case Opcode.GLOBAL_U16:
                case Opcode.GLOBAL_U16_LOAD:
                case Opcode.GLOBAL_U16_STORE:
                    OperandToU16(operands[0], ref errorOccurred);
                    break;
                case Opcode.J:
                case Opcode.JZ:
                case Opcode.IEQ_JZ:
                case Opcode.INE_JZ:
                case Opcode.IGT_JZ:
                case Opcode.IGE_JZ:
                case Opcode.ILT_JZ:
                case Opcode.ILE_JZ: // TODO
                    break;
                case Opcode.CALL: // TODO
                    break;
                case Opcode.GLOBAL_U24:
                case Opcode.GLOBAL_U24_LOAD:
                case Opcode.GLOBAL_U24_STORE:
                case Opcode.PUSH_CONST_U24:
                    OperandToU24(operands[0], ref errorOccurred);
                    break;
                case Opcode.SWITCH: // TODO
                    break;
            }

            if (errorOccurred)
            {
                // don't write the instruction to the segment if an error occurred
                codeBuilder.Drop();
            }
            else
            {
                codeBuilder.Flush();
            }
        }

        private ulong ParseOperandToU32(ScAsmParser.OperandContext operand, ref bool error)
        {
            long value = 0;
            switch (operand)
            {
                case ScAsmParser.IntegerOperandContext ctx:
                    value = ctx.integer().GetText().ParseAsInt64();
                    break;
                case ScAsmParser.FloatOperandContext ctx:
                    var floatValue = ctx.@float().GetText().ParseAsFloat();
                    value = (long)Math.Truncate(floatValue);
                    Diagnostics.AddWarning("Floating-point number truncated", Source(operand));
                    break;
                case ScAsmParser.SwitchCaseOperandContext:
                    Diagnostics.AddError("Unexpected switch-case operand", Source(operand));
                    error = true;
                    break;
                case ScAsmParser.IdentifierOperandContext: throw new InvalidOperationException();
            }

            if (value < 0)
            {
                Diagnostics.AddError("Found negative integer, expected unsigned integer", Source(operand));
                error = true;
            }

            return (ulong)value;
        }

        private void OperandToTarget(ScAsmParser.IdentifierOperandContext operand, int byteSize, bool isRelative)
        {
            codeBuilder.ConstantOrLabelTarget(operand.identifier(), byteSize, isRelative);
        }

        private void OperandToU32(ScAsmParser.OperandContext operand, ref bool error)
        {
            if (operand is ScAsmParser.IdentifierOperandContext ident)
            {
                OperandToTarget(ident, 4, false);
            }
            else
            {
                var value = ParseOperandToU32(operand, ref error);
                CheckLossOfDataInUInt(value, 4, Diagnostics, Source(operand));

                codeBuilder.U32((uint)(value & 0xFFFFFFFF));
            }
        }

        private void OperandToU24(ScAsmParser.OperandContext operand, ref bool error)
        {
            if (operand is ScAsmParser.IdentifierOperandContext ident)
            {
                OperandToTarget(ident, 3, false);
            }
            else
            {
                var value = ParseOperandToU32(operand, ref error);
                CheckLossOfDataInUInt(value, 3, Diagnostics, Source(operand));

                codeBuilder.U24((uint)(value & 0xFFFFFF));
            }
        }

        private void OperandToU16(ScAsmParser.OperandContext operand, ref bool error)
        {
            if (operand is ScAsmParser.IdentifierOperandContext ident)
            {
                OperandToTarget(ident, 2, false);
            }
            else
            {
                var value = ParseOperandToU32(operand, ref error);
                CheckLossOfDataInUInt(value, 2, Diagnostics, Source(operand));

                codeBuilder.U16((ushort)(value & 0xFFFF));
            }
        }

        private void OperandToU8(ScAsmParser.OperandContext operand, ref bool error)
        {
            if (operand is ScAsmParser.IdentifierOperandContext ident)
            {
                OperandToTarget(ident, 1, false);
            }
            else
            {
                var value = ParseOperandToU32(operand, ref error);
                CheckLossOfDataInUInt(value, 1, Diagnostics, Source(operand));

                codeBuilder.U8((byte)(value & 0xFF));
            }
        }

        private static void CheckLossOfDataInUInt(ulong value, int destByteSize, DiagnosticsReport diagnostics, SourceRange source)
        {
            var (mask, name) = destByteSize switch
            {
                1 => (0x000000FFu, "8-bit"),
                2 => (0x0000FFFFu, "16-bit"),
                3 => (0x00FFFFFFu, "24-bit"),
                4 => (0xFFFFFFFFu, "32-bit"),
                _ => throw new ArgumentOutOfRangeException(nameof(destByteSize)),
            };

            if ((value & mask) != value)
            {
                diagnostics.AddWarning($"Possible loss of data, value converted to {name} unsigned integer", source);
            }
        }

        private void WriteIntFloatDirectiveOperands(ScAsmParser.DirectiveOperandListContext operandList, bool isFloat)
        {
            foreach (var operand in operandList.directiveOperand())
            {
                switch (operand)
                {
                    case ScAsmParser.IdentifierDirectiveOperandContext identifierOperand:
                        if (TryGetConstant(identifierOperand.identifier(), out var constValue))
                        {
                            if (isFloat)
                            {
                                CurrentSegmentBuilder.Float(constValue.Float);
                            }
                            else
                            {
                                CurrentSegmentBuilder.Int((int)constValue.Integer); // TODO: check for data loss
                            }
                        }
                        break;
                    case ScAsmParser.IntegerDirectiveOperandContext integerOperand:
                        var intValue = integerOperand.integer().GetText().ParseAsInt();
                        if (isFloat)
                        {
                            CurrentSegmentBuilder.Float(intValue);
                        }
                        else
                        {
                            CurrentSegmentBuilder.Int(intValue);
                        }
                        break;
                    case ScAsmParser.FloatDirectiveOperandContext floatOperand:
                        var floatValue = floatOperand.@float().GetText().ParseAsFloat();
                        if (isFloat)
                        {
                            CurrentSegmentBuilder.Float(floatValue);
                        }
                        else
                        {
                            CurrentSegmentBuilder.Int((int)Math.Truncate(floatValue));
                        }
                        break;
                    case ScAsmParser.DupDirectiveOperandContext dupOperand:
                        int count = 0;
                        if (dupOperand.identifier() != null && TryGetConstant(dupOperand.identifier(), out var countConst))
                        {
                            count = (int)countConst.Integer; // TODO: check for data loss
                        }
                        else if (dupOperand.integer() != null)
                        {
                            count = dupOperand.integer().GetText().ParseAsInt();
                        }

                        for (int i = 0; i < count; i++)
                        {
                            WriteIntFloatDirectiveOperands(dupOperand.directiveOperandList(), isFloat);
                        }
                        break;
                }
            }
        }

        private bool TryGetConstant(ScAsmParser.IdentifierContext identifier, out ConstantValue value)
        {
            var name = identifier.GetText();
            if (Constants.TryGetValue(name, out value))
            {
                return true;
            }
            else
            {
                Diagnostics.AddError($"Undefined constant '{name}'", Source(identifier));
                return false;
            }
        }

        private bool TryGetLabel(ScAsmParser.IdentifierContext identifier, out Label value)
        {
            var name = identifier.GetText();
            if (Labels.TryGetValue(name, out value))
            {
                return true;
            }
            else
            {
                Diagnostics.AddError($"Undefined label '{name}'", Source(identifier));
                return false;
            }
        }

        /// <summary>
        /// The args are stored after the static variables, so add the static segment length to the offset of labels in the '.arg' segment.
        /// </summary>
        private void FixArgLabels()
        {
            var staticSegmentLength = staticSegmentBuilder.ByteLength / GetAddressingUnitByteSize(Segment.Static);
            var argLabels = Labels.Where(kvp => kvp.Value.Segment == Segment.Arg).ToArray();
            foreach (var (name, label) in argLabels)
            {
                Labels[name] = new Label(label.Segment, label.Offset + staticSegmentLength);
            }
        }

        private static (ScriptValue[] Statics, uint StaticsCount, uint ArgsCount) SegmentToStaticsArray(SegmentBuilder staticSegment, SegmentBuilder argSegment)
        {
            var statics = MemoryMarshal.Cast<byte, ScriptValue>(staticSegment.RawDataBuffer);
            var args = MemoryMarshal.Cast<byte, ScriptValue>(argSegment.RawDataBuffer);
            
            var combined = new ScriptValue[statics.Length + args.Length];
            statics.CopyTo(combined.AsSpan(0, statics.Length));
            args.CopyTo(combined.AsSpan(statics.Length, args.Length));
            return (combined, (uint)combined.Length, (uint)args.Length);
        }

        private static (ulong[] Natives, uint NativesCount) SegmentToNativesArray(SegmentBuilder segment, uint codeLength)
        {
            var nativeHashes = MemoryMarshal.Cast<byte, ulong>(segment.RawDataBuffer).ToArray();
            for (int i = 0; i < nativeHashes.Length; i++)
            {
                nativeHashes[i] = RotateHash(nativeHashes[i], i, codeLength);
            }
            return (nativeHashes, (uint)nativeHashes.Length);

            static ulong RotateHash(ulong hash, int index, uint codeLength)
            {
                byte rotate = (byte)(((uint)index + codeLength) & 0x3F);
                return hash >> rotate | hash << (64 - rotate);
            }
        }

        public static Assembler Assemble(TextReader input, string filePath = "tmp.sc")
        {
            var a = new Assembler(filePath);
            a.Assemble(input);
            return a;
        }

        public static StringComparer CaseInsensitiveComparer => StringComparer.OrdinalIgnoreCase;

        private static SourceRange Source(ParserRuleContext context) => SourceRange.FromTokens(context.Start, context.Stop);

        /// <summary>
        /// Adds syntax errors to diagnostics report.
        /// </summary>
        private sealed class SyntaxErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
        {
            private readonly Assembler assembler;

            public SyntaxErrorListener(Assembler assembler) => this.assembler = assembler;

            public void SyntaxError(TextWriter output, IRecognizer recognizer, TSymbol offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                var source = offendingSymbol is IToken t ?
                                SourceRange.FromTokens(t, null) :
                                new SourceRange(new SourceLocation(line, charPositionInLine),
                                                new SourceLocation(line, charPositionInLine));
                assembler.Diagnostics.AddError(msg, source);
            }
        }

        public readonly struct ConstantValue
        {
            public long Integer { get; }
            public float Float { get; }
            public bool DefinedAsFloat { get; }

            public ConstantValue(long value) => (Integer, Float, DefinedAsFloat) = (value, value, false);
            public ConstantValue(float value) => (Integer, Float, DefinedAsFloat) = ((long)Math.Truncate(value), value, true);
        }

        public readonly struct Label
        {
            public Segment Segment { get; }
            public int Offset { get; }

            public Label(Segment segment, int offset) => (Segment, Offset) = (segment, offset);
        }

        private sealed class CodeBuilder
        {
            private readonly SegmentBuilder segment;
            private readonly List<byte> buffer = new();
            private readonly List<UnresolvedConstantOrLabelTarget> labelTargets = new List<UnresolvedConstantOrLabelTarget>();
            private readonly List<int> labelTargetsInCurrentInstruction = new List<int>(); // index of labelTargets

            public string? Label { get; set; }
            public CodeGenOptions Options { get; } = new(includeFunctionNames: true);

            public CodeBuilder(SegmentBuilder segment) => this.segment = segment;

            public void U8(byte v)
            {
                buffer.Add(v);
            }

            public void U16(ushort v)
            {
                buffer.Add((byte)(v & 0xFF));
                buffer.Add((byte)(v >> 8));
            }

            public void S16(short v) => U16(unchecked((ushort)v));

            public void U32(uint v)
            {
                buffer.Add((byte)(v & 0xFF));
                buffer.Add((byte)((v >> 8) & 0xFF));
                buffer.Add((byte)((v >> 16) & 0xFF));
                buffer.Add((byte)(v >> 24));
            }

            public void U24(uint v)
            {
                buffer.Add((byte)(v & 0xFF));
                buffer.Add((byte)((v >> 8) & 0xFF));
                buffer.Add((byte)((v >> 16) & 0xFF));
            }

            public unsafe void F32(float v) => U32(*(uint*)&v);

            public void ConstantOrLabelTarget(ScAsmParser.IdentifierContext identifier, int byteSize, bool isRelative)
            {
                labelTargetsInCurrentInstruction.Add(labelTargets.Count);
                labelTargets.Add(new(identifier, segment.Length + buffer.Count, byteSize, isRelative));
                for (int i = 0; i < byteSize; i++)
                {
                    buffer.Add(0); // empty, will be filled later when we know the offsets of all labels
                }
            }

            public void Opcode(Opcode v) => U8((byte)v);

            /// <summary>
            /// Clears the current instruction buffer.
            /// </summary>
            public void Drop()
            {
                buffer.Clear();
                labelTargetsInCurrentInstruction.Clear();
                Label = null;
            }

            /// <summary>
            /// Writes the current instruction buffer to the segment.
            /// </summary>
            public void Flush()
            {
                int offset = (int)(segment.Length & (Script.MaxPageLength - 1));

                Opcode opcode = (Opcode)buffer[0];

                // At page boundary a NOP may be required for the interpreter to switch to the next page,
                // the interpreter only does this with control flow instructions and NOP
                // If the NOP is needed, skip 1 byte at the end of the page
                bool needsNopAtBoundary = !opcode.IsControlFlow() &&
                                          opcode != ScriptAssembly.Opcode.NOP;

                if (offset + buffer.Count > (Script.MaxPageLength - (needsNopAtBoundary ? 1u : 0))) // the instruction doesn't fit in the current page
                {
                    var bytesUntilNextPage = (int)Script.MaxPageLength - offset; // padding needed to skip to the next page
                    var requiredNops = bytesUntilNextPage;

                    const int JumpInstructionSize = 3;
                    if (bytesUntilNextPage > JumpInstructionSize)
                    {
                        // if there is enough space for a J instruction, add it to jump to the next page
                        short relIP = (short)(Script.MaxPageLength - (offset + JumpInstructionSize)); // get how many bytes until the next page
                        segment.Byte((byte)ScriptAssembly.Opcode.J);
                        segment.Byte((byte)(relIP & 0xFF));
                        segment.Byte((byte)(relIP >> 8));
                        requiredNops -= JumpInstructionSize;
                    }

                    // NOP what is left of the current page
                    segment.Bytes(new byte[requiredNops]);

                    // fix IPs of label targets in the current instruction
                    foreach (int i in labelTargetsInCurrentInstruction)
                    {
                        var orig = labelTargets[i];
                        labelTargets[i] = new(orig.Identifier, orig.Offset + bytesUntilNextPage, orig.ByteSize, orig.IsRelative);
                    }
                }

                segment.Bytes(CollectionsMarshal.AsSpan(buffer));
                Drop();
            }

            public void ResolveLabelTargets(Dictionary<string, Label> labels, Dictionary<string, ConstantValue> constants, DiagnosticsReport diagnostics)
            {
                foreach (var labelTarget in labelTargets)
                {
                    labelTarget.Resolve(segment, labels, constants, diagnostics);
                }
                labelTargets.Clear();
            }
        }

        private readonly struct UnresolvedConstantOrLabelTarget
        {
            public ScAsmParser.IdentifierContext Identifier { get; }
            public int Offset { get; }
            public int ByteSize { get; }
            public bool IsRelative { get; }

            public UnresolvedConstantOrLabelTarget(ScAsmParser.IdentifierContext identifier, int offset, int byteSize, bool isRelative)
                => (Identifier, Offset, ByteSize, IsRelative) = (identifier, offset, byteSize, isRelative);

            public void Resolve(SegmentBuilder segment, Dictionary<string, Label> labels, Dictionary<string, ConstantValue> constants, DiagnosticsReport diagnostics)
            {
                int val = GetValue(labels, constants, diagnostics);

                if (IsRelative)
                {
                    val -= Offset + ByteSize;
                }

                segment.RawDataBuffer[Offset + 0] = (byte)(val & 0xFF);
                if (ByteSize >= 2)
                {
                    segment.RawDataBuffer[Offset + 1] = (byte)((val >> 8) & 0xFF);
                }
                if (ByteSize >= 3)
                {
                    segment.RawDataBuffer[Offset + 2] = (byte)((val >> 16) & 0xFF);
                }
                if (ByteSize >= 4)
                {
                    segment.RawDataBuffer[Offset + 3] = (byte)((val >> 24) & 0xFF);
                }
            }

            private int GetValue(Dictionary<string, Label> labels, Dictionary<string, ConstantValue> constants, DiagnosticsReport diagnostics)
            {
                var name = Identifier.GetText();
                if (labels.TryGetValue(name, out var label))
                {
                    return label.Offset;
                }

                if (constants.TryGetValue(name, out var constValue))
                {
                    if (constValue.DefinedAsFloat)
                    {
                        diagnostics.AddWarning("Floating-point number truncated", Source(Identifier));
                    }

                    CheckLossOfDataInUInt((ulong)constValue.Integer, ByteSize, diagnostics, Source(Identifier)); // TODO: check for negative value
                    return (int)constValue.Integer;
                }

                diagnostics.AddError($"'{name}' is undefined", Source(Identifier));
                return 0;
            }
        }
    }
}

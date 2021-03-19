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
            codeBuilder.FixupLabelTargets(Labels);

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
            var offset = (int)(CurrentSegmentBuilder.ByteLength / GetAddressingUnitByteSize(CurrentSegment));

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
                                CurrentSegmentBuilder.Int(constValue.Integer);
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
                            count = countConst.Integer;
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

        /// <summary>
        /// The args are stored after the static variables, so add the static segment length to the offset of labels in the '.arg' segment.
        /// </summary>
        private void FixArgLabels()
        {
            var staticSegmentLength = (int)(staticSegmentBuilder.ByteLength / GetAddressingUnitByteSize(Segment.Static));
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

        public struct ConstantValue
        {
            public int Integer { get; }
            public float Float { get; }

            public ConstantValue(int value) => (Integer, Float) = (value, value);
            public ConstantValue(float value) => (Integer, Float) = ((int)Math.Truncate(value), value);
        }

        public struct Label
        {
            public Segment Segment { get; }
            public int Offset { get; }

            public Label(Segment segment, int offset) => (Segment, Offset) = (segment, offset);
        }

        private sealed class CodeBuilder : IByteCodeBuilder
        {
            private readonly SegmentBuilder segment;
            private readonly List<byte> buffer = new();
            private readonly List<(string TargetFunctionName, int IP)> absoluteLabelTargets = new List<(string, int)>();
            private readonly List<(string TargetLabel, int IP)> relativeLabelTargets = new List<(string, int)>();
            private readonly List<int> absoluteTargetsInCurrentInstruction = new List<int>(); // index of functionTargets
            private readonly List<int> relativeLabelTargetsInCurrentInstruction = new List<int>(); // index of labelTargets

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

            public void AbsoluteLabelTarget(string function)
            {
                if (string.IsNullOrWhiteSpace(function))
                {
                    throw new ArgumentException("null or empty function name", nameof(function));
                }

                absoluteTargetsInCurrentInstruction.Add(absoluteLabelTargets.Count);
                absoluteLabelTargets.Add((function, segment.Length + buffer.Count));
                buffer.Add(0); // empty, will be filled later when we know the offsets of all labels
                buffer.Add(0);
                buffer.Add(0);
            }

            public void RelativeLabelTarget(string label)
            {
                if (string.IsNullOrWhiteSpace(label))
                {
                    throw new ArgumentException("null or empty label", nameof(label));
                }

                relativeLabelTargetsInCurrentInstruction.Add(relativeLabelTargets.Count);
                relativeLabelTargets.Add((label, segment.Length + buffer.Count));
                buffer.Add(0); // empty, will be filled later when we know the offsets of all labels
                buffer.Add(0);
            }

            public void Opcode(Opcode v) => U8((byte)v);

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

                    // fix IPs of label/function targets in the current instruction
                    foreach (int i in absoluteTargetsInCurrentInstruction)
                    {
                        absoluteLabelTargets[i] = (absoluteLabelTargets[i].TargetFunctionName, absoluteLabelTargets[i].IP + bytesUntilNextPage);
                    }
                    foreach (int i in relativeLabelTargetsInCurrentInstruction)
                    {
                        relativeLabelTargets[i] = (relativeLabelTargets[i].TargetLabel, relativeLabelTargets[i].IP + bytesUntilNextPage);
                    }
                }

                segment.Bytes(CollectionsMarshal.AsSpan(buffer));
                buffer.Clear();
                absoluteTargetsInCurrentInstruction.Clear();
                relativeLabelTargetsInCurrentInstruction.Clear();
                Label = null;
            }

            private int GetLabelIP(Dictionary<string, Label> labels, string label)
            {
                if (!labels.TryGetValue(label, out var labelInfo))
                {
                    throw new ArgumentException($"Unknown label '{label}'", nameof(label));
                }

                if (labelInfo.Segment != Segment.Code)
                {
                    throw new ArgumentException($"Label '{label}' is not in code segment", nameof(label));
                }

                return labelInfo.Offset;
            }

            private void FixupAbsoluteLabelTargets(Dictionary<string, Label> labels)
            {
                foreach (var (targetLabel, targetIP) in absoluteLabelTargets)
                {
                    int ip = GetLabelIP(labels, targetLabel);

                    segment.RawDataBuffer[targetIP + 0] = (byte)(ip & 0xFF);
                    segment.RawDataBuffer[targetIP + 1] = (byte)((ip >> 8) & 0xFF);
                    segment.RawDataBuffer[targetIP + 2] = (byte)(ip >> 16);
                }

                absoluteLabelTargets.Clear();
            }

            private void FixupRelativeLabelTargets(Dictionary<string, Label> labels)
            {
                foreach (var (targetLabel, targetIP) in relativeLabelTargets)
                {
                    int ip = GetLabelIP(labels, targetLabel);

                    short relIP = (short)(ip - (targetIP + 2));
                    segment.RawDataBuffer[targetIP + 0] = (byte)(relIP & 0xFF);
                    segment.RawDataBuffer[targetIP + 1] = (byte)(relIP >> 8);
                }

                relativeLabelTargets.Clear();
            }

            public void FixupLabelTargets(Dictionary<string, Label> labels)
            {
                FixupAbsoluteLabelTargets(labels);
                FixupRelativeLabelTargets(labels);
            }
        }
    }
}

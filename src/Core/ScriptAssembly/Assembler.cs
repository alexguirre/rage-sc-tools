#nullable enable
namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using Antlr4.Runtime;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Grammar;

    public readonly struct AssemblerOptions
    {
        /// <summary>
        /// If <c>true</c>, the ENTER instructions will be encoded including the closest label declared before it.
        /// </summary>
        public bool IncludeFunctionNames { get; init; }
    }

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

        private readonly List<Instruction> instructions = new();

        public IAssemblySource AssemblySource { get; }
        public DiagnosticsReport Diagnostics { get; }
        public Script OutputScript { get; }
        public Dictionary<string, ConstantValue> Constants { get; }
        public Dictionary<string, Label> Labels { get; }
        public bool HasScriptName { get; private set; }
        public bool HasScriptHash { get; private set; }
        public bool HasGlobalBlock { get; private set; }
        public NativeDB? NativeDB { get; set; }
        public AssemblerOptions Options { get; set; }

        public Assembler(IAssemblySource source)
        {
            AssemblySource = source;
            Diagnostics = new();
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

        public void Assemble()
        {
            FirstPass();
            SecondPass();

            OutputScript.CodePages = codeSegmentBuilder.Length != 0 ? codeSegmentBuilder.ToPages<byte>() : null;
            OutputScript.CodeLength = OutputScript.CodePages?.Length ?? 0;

            OutputScript.GlobalsPages = globalSegmentBuilder.Length != 0 ? globalSegmentBuilder.ToPages<ScriptValue>() : null;
            OutputScript.GlobalsLength = OutputScript.GlobalsPages?.Length ?? 0;

            (OutputScript.Statics, OutputScript.StaticsCount, OutputScript.ArgsCount) = SegmentToStaticsArray(staticSegmentBuilder, argSegmentBuilder);
            (OutputScript.Natives, OutputScript.NativesCount) = SegmentToNativesArray(includeSegmentBuilder, OutputScript.CodeLength);

            OutputScript.StringsPages = stringSegmentBuilder.Length != 0 ? stringSegmentBuilder.ToPages<byte>() : null;
            OutputScript.StringsLength = OutputScript.StringsPages?.Length ?? 0;
        }

        /// <summary>
        /// Traverses the parse tree. Allocates the space needed by each segment, initializes non-code segments, stores labels offsets
        /// and collects the instructions from the code segment.
        /// </summary>
        private void FirstPass()
        {
            AssemblySource.Produce(Diagnostics, (_, line) => ProcessLine(line));
            FixArgLabels();
        }

        /// <summary>
        /// Assembles the instructions collected in the first pass, since now it knows the offsets of all labels.
        /// </summary>
        private void SecondPass()
        {
            foreach (var inst in instructions)
            {
                AssembleInstruction(inst);
            }
        }

        private void ProcessLine(ScAsmParser.LineContext line)
        {
            var label = line.label();
            var directive = line.directive();
            var instruction = line.instruction();

            if (label is not null)
            {
                ProcessLabel(label);
            }

            if (directive is not null)
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

            if (Constants.ContainsKey(name))
            {
                Diagnostics.AddError($"Constant named '{name}' already defined", Source(label));
            }
            else if (!Labels.TryAdd(name, new Label(CurrentSegment, offset)))
            {
                Diagnostics.AddError($"Label '{name}' already defined", Source(label));
            }
        }

        private void ChangeSegment(ScAsmParser.DirectiveContext directive)
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
                case ScAsmParser.GlobalSegmentDirectiveContext or
                     ScAsmParser.StaticSegmentDirectiveContext or
                     ScAsmParser.ArgSegmentDirectiveContext or
                     ScAsmParser.StringSegmentDirectiveContext or
                     ScAsmParser.CodeSegmentDirectiveContext or
                     ScAsmParser.IncludeSegmentDirectiveContext:
                    ChangeSegment(directive);
                    break;

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
                    var constNameStr = constName.GetText();
                    var constInteger = constDirective.integer();
                    var constFloat = constDirective.@float();

                    var constValue = constInteger != null ?
                                        new ConstantValue(constInteger.GetText().ParseAsInt64()) :
                                        new ConstantValue(constFloat.GetText().ParseAsFloat());

                    if (Labels.ContainsKey(constNameStr))
                    {
                        Diagnostics.AddError($"Label named '{constNameStr}' already defined", Source(constName));
                    }
                    else if (!Constants.TryAdd(constNameStr, constValue))
                    {
                        Diagnostics.AddError($"Constant '{constNameStr}' already defined", Source(constName));
                    }
                    break;
                case ScAsmParser.IntDirectiveContext intDirective:
                    WriteIntFloatDirectiveOperands(intDirective.directiveOperandList(), isFloat: false, isInt64: false);
                    break;
                case ScAsmParser.Int64DirectiveContext int64Directive:
                    WriteIntFloatDirectiveOperands(int64Directive.directiveOperandList(), isFloat: false, isInt64: true);
                    break;
                case ScAsmParser.FloatDirectiveContext floatDirective:
                    WriteIntFloatDirectiveOperands(floatDirective.directiveOperandList(), isFloat: true, isInt64: false);
                    break;
                case ScAsmParser.StrDirectiveContext strDirective:
                    CurrentSegmentBuilder.String(strDirective.@string().GetText()[1..^1].Unescape());
                    break;
                case ScAsmParser.NativeDirectiveContext nativeDirective:
                    var hash = nativeDirective.integer().GetText().ParseAsUInt64();
                    if (NativeDB is not null)
                    {
                        var translatedHash = NativeDB.TranslateHash(hash, GameBuild.Latest);
                        if (translatedHash == 0)
                        {
                            Diagnostics.AddWarning($"Unknown native hash '{hash:X16}'", Source(nativeDirective.integer()));
                        }
                        else
                        {
                            hash = translatedHash;
                        }
                    }
                    CurrentSegmentBuilder.UInt64(hash);
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
                Diagnostics.AddError($"Unknown opcode '{instruction.opcode().GetText()}'", Source(instruction.opcode()));
                return;
            }

            var expectedNumOperands = opcode.NumberOfOperands();
            var operands = instruction.operandList()?.operand() ?? Array.Empty<ScAsmParser.OperandContext>();
            if (expectedNumOperands != -1 && operands.Length != expectedNumOperands)
            {
                Diagnostics.AddError($"Expected {expectedNumOperands} operands for opcode {opcode} but found {operands.Length} operands", Source(instruction));
                return;
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
                    codeBuilder.U8(0);
                    break;
                case Opcode.PUSH_CONST_U8_U8:
                case Opcode.LEAVE:
                    codeBuilder.U8(0);
                    codeBuilder.U8(0);
                    break;
                case Opcode.PUSH_CONST_U8_U8_U8:
                    codeBuilder.U8(0);
                    codeBuilder.U8(0);
                    codeBuilder.U8(0);
                    break;
                case Opcode.PUSH_CONST_U32:
                    codeBuilder.U32(0);
                    break;
                case Opcode.PUSH_CONST_F:
                    codeBuilder.F32(0);
                    break;
                case Opcode.NATIVE:
                    codeBuilder.U8(0);
                    codeBuilder.U8(0);
                    codeBuilder.U8(0);
                    break;
                case Opcode.ENTER:
                    var enterAddr = codeSegmentBuilder.Length;
                    codeBuilder.U8(0);
                    codeBuilder.U16(0);
                    if (Options.IncludeFunctionNames)
                    {
                        var (name, lbl) = Labels.Where(lbl => lbl.Value.Segment is Segment.Code && lbl.Value.Offset <= enterAddr)
                                                .OrderByDescending(lbl => lbl.Value.Offset)
                                                .FirstOrDefault();
                        if (lbl.Segment is Segment.Code)
                        {
                            var nameBytes = System.Text.Encoding.UTF8.GetBytes(name).AsSpan();
                            nameBytes = nameBytes.Slice(0, Math.Min(nameBytes.Length, byte.MaxValue - 1)); // limit length to 255 (including null terminators)
                            codeBuilder.U8((byte)(nameBytes.Length + 1));
                            codeBuilder.Bytes(nameBytes);
                            codeBuilder.U8(0); // null terminator

                        }
                        else
                        {
                            // no label found
                            codeBuilder.U8(0);
                        }
                    }
                    else
                    {
                        codeBuilder.U8(0);
                    }
                    break;
                case Opcode.PUSH_CONST_S16:
                case Opcode.IADD_S16:
                case Opcode.IMUL_S16:
                case Opcode.IOFFSET_S16:
                case Opcode.IOFFSET_S16_LOAD:
                case Opcode.IOFFSET_S16_STORE:
                    codeBuilder.S16(0);
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
                    codeBuilder.U16(0);
                    break;
                case Opcode.J:
                case Opcode.JZ:
                case Opcode.IEQ_JZ:
                case Opcode.INE_JZ:
                case Opcode.IGT_JZ:
                case Opcode.IGE_JZ:
                case Opcode.ILT_JZ:
                case Opcode.ILE_JZ:
                    codeBuilder.S16(0);
                    break;
                case Opcode.CALL:
                case Opcode.GLOBAL_U24:
                case Opcode.GLOBAL_U24_LOAD:
                case Opcode.GLOBAL_U24_STORE:
                case Opcode.PUSH_CONST_U24:
                    codeBuilder.U24(0);
                    break;
                case Opcode.SWITCH:
                    codeBuilder.U8(0);
                    for (int i = 0; i < operands.Length; i++)
                    {
                        codeBuilder.U32(0);
                        codeBuilder.S16(0);
                    }
                    break;
            }

            var (offset, length) = codeBuilder.Flush();
            instructions.Add(new(AssemblySource.FilePath, instruction, opcode, offset, length));
        }

        private void AssembleInstruction(Instruction instruction)
        {
            var span = GetInstructionSpan(instruction);
            span = span[1..]; // skip opcode byte, already set in the first pass

            var operands = instruction.Operands;

            switch (instruction.Opcode)
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
                    OperandToU8(span[0..], operands[0]);
                    break;
                case Opcode.PUSH_CONST_U8_U8:
                case Opcode.LEAVE:
                    OperandToU8(span[0..], operands[0]);
                    OperandToU8(span[1..], operands[1]);
                    break;
                case Opcode.PUSH_CONST_U8_U8_U8:
                    OperandToU8(span[0..], operands[0]);
                    OperandToU8(span[1..], operands[1]);
                    OperandToU8(span[2..], operands[2]);
                    break;
                case Opcode.PUSH_CONST_U32:
                    OperandToU32(span[0..], operands[0]);
                    break;
                case Opcode.PUSH_CONST_F:
                    OperandToF32(span[0..], operands[0]);
                    break;
                case Opcode.NATIVE:
                    var argCount = ParseOperandToUInt(operands[0]);
                    var returnCount = ParseOperandToUInt(operands[1]);
                    var nativeIndex = ParseOperandToUInt(operands[2]);

                    CheckLossOfDataInUInt(argCount, maxBits: 6, Diagnostics, operands[0].Source);
                    CheckLossOfDataInUInt(returnCount, maxBits: 2, Diagnostics, operands[1].Source);
                    CheckLossOfDataInUInt(nativeIndex, maxBits: 16, Diagnostics, operands[2].Source);

                    span[0] = (byte)((argCount & 0x3F) << 2 | (returnCount & 0x3));
                    span[1] = (byte)((nativeIndex >> 8) & 0xFF);
                    span[2] = (byte)(nativeIndex & 0xFF);
                    break;
                case Opcode.ENTER:
                    OperandToU8(span[0..], operands[0]);
                    OperandToU16(span[1..], operands[1]);
                    // note: label name is already written in ProcessInstruction
                    break;
                case Opcode.PUSH_CONST_S16:
                case Opcode.IADD_S16:
                case Opcode.IMUL_S16:
                case Opcode.IOFFSET_S16:
                case Opcode.IOFFSET_S16_LOAD:
                case Opcode.IOFFSET_S16_STORE:
                    OperandToS16(span[0..], operands[0]);
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
                    OperandToU16(span[0..], operands[0]);
                    break;
                case Opcode.J:
                case Opcode.JZ:
                case Opcode.IEQ_JZ:
                case Opcode.INE_JZ:
                case Opcode.IGT_JZ:
                case Opcode.IGE_JZ:
                case Opcode.ILT_JZ:
                case Opcode.ILE_JZ:
                    OperandToRelativeLabelOffsetOrS16(span[0..], instruction.Offset + 1, operands[0]);
                    break;
                case Opcode.CALL:
                case Opcode.GLOBAL_U24:
                case Opcode.GLOBAL_U24_LOAD:
                case Opcode.GLOBAL_U24_STORE:
                case Opcode.PUSH_CONST_U24:
                    OperandToU24(span[0..], operands[0]);
                    break;
                case Opcode.SWITCH:
                    if (operands.Length > byte.MaxValue)
                    {
                        Diagnostics.AddError($"Too many switch-cases, maximum number is {byte.MaxValue}", instruction.Source);
                    }

                    span[0] = (byte)operands.Length;
                    span = span[1..];
                    for (int i = 0, jumpToOperandOffset = instruction.Offset + 1 /*opcode*/ + 1 /*total case count*/ + 4 /*case value*/;
                        i < operands.Length;
                        i++, jumpToOperandOffset += 6, span = span[6..])
                    {
                        if (operands[i].Type is InstructionOperandType.SwitchCase)
                        {
                            // TODO: warning if cases are repeated
                            OperandToU32(span[0..], operands[i].SwitchCaseOperands[0]);
                            OperandToRelativeLabelOffsetOrS16(span[4..], jumpToOperandOffset, operands[i].SwitchCaseOperands[1]);
                        }
                        else
                        {
                            Diagnostics.AddError("Expected switch-case operand", operands[i].Source);
                        }
                    }
                    break;
            }
        }

        private Span<byte> GetInstructionSpan(Instruction instruction)
            => codeSegmentBuilder.RawDataBuffer.Slice(instruction.Offset, instruction.Length);

        private ulong ParseOperandToUInt(InstructionOperand operand)
        {
            long value = 0;
            switch (operand.Type)
            {
                case InstructionOperandType.Integer:
                    value = operand.Text.Span.ParseAsInt64();
                    break;
                case InstructionOperandType.Float:
                    var floatValue = operand.Text.Span.ParseAsFloat();
                    value = (long)Math.Truncate(floatValue);
                    Diagnostics.AddWarning("Floating-point number truncated", operand.Source);
                    break;
                case InstructionOperandType.SwitchCase:
                    Diagnostics.AddError("Unexpected switch-case operand", operand.Source);
                    break;
                case InstructionOperandType.Identifier:
                    var name = operand.Text.ToString();
                    if (Labels.TryGetValue(name, out var label))
                    {
                        value = label.Offset;
                    }
                    else if (Constants.TryGetValue(name, out var constValue))
                    {
                        if (constValue.DefinedAsFloat)
                        {
                            Diagnostics.AddWarning("Floating-point number truncated", operand.Source);
                        }

                        value = constValue.Integer;
                    }
                    else
                    {
                        Diagnostics.AddError($"'{name}' is undefined", operand.Source);
                    }
                    break;
            }

            if (value < 0)
            {
                Diagnostics.AddError("Found negative integer, expected unsigned integer", operand.Source);
            }

            return (ulong)value;
        }

        private long ParseOperandToInt(InstructionOperand operand)
        {
            long value = 0;
            switch (operand.Type)
            {
                case InstructionOperandType.Integer:
                    value = operand.Text.Span.ParseAsInt64();
                    break;
                case InstructionOperandType.Float:
                    var floatValue = operand.Text.Span.ParseAsFloat();
                    value = (long)Math.Truncate(floatValue);
                    Diagnostics.AddWarning("Floating-point number truncated", operand.Source);
                    break;
                case InstructionOperandType.SwitchCase:
                    Diagnostics.AddError("Unexpected switch-case operand", operand.Source);
                    break;
                case InstructionOperandType.Identifier:
                    var name = operand.Text.ToString();
                    if (Labels.TryGetValue(name, out var label))
                    {
                        value = label.Offset;
                    }
                    else if (Constants.TryGetValue(name, out var constValue))
                    {
                        if (constValue.DefinedAsFloat)
                        {
                            Diagnostics.AddWarning("Floating-point number truncated", operand.Source);
                        }

                        value = constValue.Integer;
                    }
                    else
                    {
                        Diagnostics.AddError($"'{name}' is undefined", operand.Source);
                    }
                    break;
            }

            return value;
        }

        private float ParseOperandToFloat(InstructionOperand operand)
        {
            float value = 0;
            switch (operand.Type)
            {
                case InstructionOperandType.Integer:
                    value = operand.Text.Span.ParseAsFloat();
                    break;
                case InstructionOperandType.Float:
                    value = operand.Text.Span.ParseAsFloat();
                    break;
                case InstructionOperandType.SwitchCase:
                    Diagnostics.AddError("Unexpected switch-case operand", operand.Source);
                    break;
                case InstructionOperandType.Identifier:
                    var name = operand.Text.ToString();
                    if (Labels.TryGetValue(name, out _))
                    {
                        Diagnostics.AddError($"Expected floating-point number, cannot use label '{name}'", operand.Source);
                    }
                    else if (Constants.TryGetValue(name, out var constValue))
                    {
                        value = constValue.Float;
                    }
                    else
                    {
                        Diagnostics.AddError($"'{name}' is undefined", operand.Source);
                    }
                    break;
            }

            return value;
        }

        private void OperandToF32(Span<byte> dest, InstructionOperand operand)
        {
            var value = ParseOperandToFloat(operand);

            MemoryMarshal.Write(dest, ref value);
        }

        private void OperandToU32(Span<byte> dest, InstructionOperand operand)
        {
            var value = ParseOperandToUInt(operand);
            CheckLossOfDataInUInt(value, 32, Diagnostics, operand.Source);

            dest[0] = (byte)(value & 0xFF);
            dest[1] = (byte)((value >> 8) & 0xFF);
            dest[2] = (byte)((value >> 16) & 0xFF);
            dest[3] = (byte)((value >> 24) & 0xFF);
        }

        private void OperandToU24(Span<byte> dest, InstructionOperand operand)
        {
            var value = ParseOperandToUInt(operand);
            CheckLossOfDataInUInt(value, 24, Diagnostics, operand.Source);

            dest[0] = (byte)(value & 0xFF);
            dest[1] = (byte)((value >> 8) & 0xFF);
            dest[2] = (byte)((value >> 16) & 0xFF);
        }

        private void OperandToU16(Span<byte> dest, InstructionOperand operand)
        {
            var value = ParseOperandToUInt(operand);
            CheckLossOfDataInUInt(value, 16, Diagnostics, operand.Source);

            dest[0] = (byte)(value & 0xFF);
            dest[1] = (byte)((value >> 8) & 0xFF);
        }

        private void OperandToU8(Span<byte> dest, InstructionOperand operand)
        {
            var value = ParseOperandToUInt(operand);
            CheckLossOfDataInUInt(value, 8, Diagnostics, operand.Source);

            dest[0] = (byte)(value & 0xFF);
        }

        private void OperandToS16(Span<byte> dest, InstructionOperand operand)
        {
            var value = ParseOperandToInt(operand);
            CheckLossOfDataInInt(value, 16, Diagnostics, operand.Source);

            dest[0] = (byte)(value & 0xFF);
            dest[1] = (byte)((value >> 8) & 0xFF);
        }

        private void OperandToRelativeLabelOffsetOrS16(Span<byte> dest, int operandOffset, InstructionOperand operand)
        {
            if (operand.Type is InstructionOperandType.Identifier && Labels.TryGetValue(operand.Text.ToString(), out var label))
            {
                if (label.Segment != Segment.Code)
                {
                    Diagnostics.AddError($"Cannot jump to label '{operand.Text.ToString()}' outside code segment", operand.Source);
                    return;
                }

                var absOffset = label.Offset;
                var relOffset = absOffset - (operandOffset + 2);
                if (relOffset < short.MinValue || relOffset > short.MaxValue)
                {
                    Diagnostics.AddError($"Label '{operand.Text.ToString()}' is too far", operand.Source);
                    return;
                }

                dest[0] = (byte)(relOffset & 0xFF);
                dest[1] = (byte)((relOffset >> 8) & 0xFF);
            }
            else
            {
                OperandToS16(dest, operand);
            }
        }

        private static void CheckLossOfDataInUInt(ulong value, int maxBits, DiagnosticsReport diagnostics, SourceRange source)
        {
            var maxValue = (1UL << maxBits) - 1;

            if (value > maxValue)
            {
                diagnostics.AddWarning($"Possible loss of data, value converted to {maxBits}-bit unsigned integer (value was {value}, range is from 0 to {maxValue})", source);
            }
        }

        private static void CheckLossOfDataInInt(long value, int maxBits, DiagnosticsReport diagnostics, SourceRange source)
        {
            var maxValue = (1L << (maxBits - 1)) - 1;
            var minValue = -(1L << (maxBits - 1));

            if (value < minValue || value > maxValue)
            {
                diagnostics.AddWarning($"Possible loss of data, value converted to {maxBits}-bit signed integer (value was {value}, range is from  {minValue} to {maxValue})", source);
            }
        }

        private void WriteIntFloatDirectiveOperands(ScAsmParser.DirectiveOperandListContext operandList, bool isFloat, bool isInt64)
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
                                if (isInt64)
                                {
                                    CurrentSegmentBuilder.Int64(constValue.Integer);
                                }
                                else
                                {
                                    CurrentSegmentBuilder.Int((int)constValue.Integer); // TODO: check for data loss
                                }
                            }
                        }
                        break;
                    case ScAsmParser.IntegerDirectiveOperandContext integerOperand:
                        var intValue = integerOperand.integer().GetText().ParseAsInt64();
                        if (isFloat)
                        {
                            CurrentSegmentBuilder.Float(intValue);
                        }
                        else
                        {
                            if (isInt64)
                            {
                                CurrentSegmentBuilder.Int64(intValue);
                            }
                            else
                            {
                                CurrentSegmentBuilder.Int((int)intValue); // TODO: check for data loss
                            }
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
                            if (isInt64)
                            {
                                CurrentSegmentBuilder.Int64((long)Math.Truncate(floatValue));
                            }
                            else
                            {
                                CurrentSegmentBuilder.Int((int)Math.Truncate(floatValue));
                            }
                        }
                        break;
                    case ScAsmParser.DupDirectiveOperandContext dupOperand:
                        long count = 0;
                        if (dupOperand.identifier() != null && TryGetConstant(dupOperand.identifier(), out var countConst))
                        {
                            count = countConst.Integer;
                        }
                        else if (dupOperand.integer() != null)
                        {
                            count = dupOperand.integer().GetText().ParseAsInt64();
                        }

                        for (long i = 0; i < count; i++)
                        {
                            WriteIntFloatDirectiveOperands(dupOperand.directiveOperandList(), isFloat, isInt64);
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

        private SourceRange Source(ParserRuleContext context) => Source(AssemblySource.FilePath, context);
        private static SourceRange Source(string filePath, ParserRuleContext context) => SourceRange.FromTokens(filePath, context.Start, context.Stop);

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
                nativeHashes[i] = Script.EncodeNativeHash(nativeHashes[i], i, codeLength);
            }
            return (nativeHashes, (uint)nativeHashes.Length);
        }

        public static Assembler Assemble(TextReader input, string filePath = "tmp.sc", NativeDB? nativeDB = null, AssemblerOptions options = default)
        {
            var a = new Assembler(new TextAssemblySource(input, filePath)) { NativeDB = nativeDB, Options = options };
            a.Assemble();
            return a;
        }

        public static StringComparer CaseInsensitiveComparer => StringComparer.OrdinalIgnoreCase;

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

        public readonly struct Instruction
        {
            public ImmutableArray<InstructionOperand> Operands { get; }
            public SourceRange Source { get; }
            public Opcode Opcode { get; }
            public int Offset { get; }
            public int Length { get; }

            public Instruction(string filePath, ScAsmParser.InstructionContext context, Opcode opcode, int offset, int length)
            {
                (Source, Opcode, Offset, Length) = (Source(filePath, context), opcode, offset, length);

                var operands = context.operandList()?.operand() ?? Array.Empty<ScAsmParser.OperandContext>();
                var operandsBuilder = ImmutableArray.CreateBuilder<InstructionOperand>(operands.Length);
                foreach (var operand in operands)
                {
                    operandsBuilder.Add(CreateOperand(filePath, operand));
                }
                Operands = operandsBuilder.MoveToImmutable();


                static InstructionOperand CreateOperand(string filePath, ScAsmParser.OperandContext operand)
                {
                    var start = (LightToken)operand.Start;
                    var stop = (LightToken)operand.Stop;
                    var switchCaseOperands = operand is ScAsmParser.SwitchCaseOperandContext switchCase ?
                                          new[] { CreateOperand(filePath, switchCase.value), CreateOperand(filePath, switchCase.jumpTo) } :
                                          Array.Empty<InstructionOperand>();

                    return new(
                        operand switch
                        {
                            ScAsmParser.IntegerOperandContext => InstructionOperandType.Integer,
                            ScAsmParser.FloatOperandContext => InstructionOperandType.Float,
                            ScAsmParser.SwitchCaseOperandContext => InstructionOperandType.SwitchCase,
                            ScAsmParser.IdentifierOperandContext => InstructionOperandType.Identifier,
                            _ => throw new InvalidOperationException(),
                        },
                        ((LightInputStream)start.InputStream).GetTextMemory(start.StartIndex, stop.StopIndex),
                        Source(filePath, operand),
                        switchCaseOperands
                    );
                }
            }
        }

        public enum InstructionOperandType
        {
            Integer, Float, SwitchCase, Identifier
        }

        public readonly struct InstructionOperand
        {
            public InstructionOperandType Type { get; }
            public ReadOnlyMemory<char> Text { get; }
            public SourceRange Source { get; }
            public InstructionOperand[] SwitchCaseOperands { get; } // cannot use ImmutableArray here, causes TypeLoadException due to recursive static ctor calls

            public InstructionOperand(InstructionOperandType type, ReadOnlyMemory<char> text, SourceRange source, InstructionOperand[] switchCaseOperands)
            {
                Type = type;
                Text = text;
                Source = source;
                SwitchCaseOperands = switchCaseOperands;
            }
        }

        private sealed class CodeBuilder
        {
            private readonly SegmentBuilder segment;
            private readonly List<byte> buffer = new();

            public CodeBuilder(SegmentBuilder segment) => this.segment = segment;

            public void Bytes(ReadOnlySpan<byte> bytes)
            {
                foreach (var b in bytes)
                {
                    buffer.Add(b);
                }
            }

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

            public void Opcode(Opcode v) => U8((byte)v);

            /// <summary>
            /// Clears the current instruction buffer.
            /// </summary>
            public void Drop()
            {
                buffer.Clear();
            }

            /// <summary>
            /// Writes the current instruction buffer to the segment.
            /// </summary>
            public (int InstructionOffset, int InstructionLength) Flush()
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
                }

                var instOffset = segment.Length;
                var instLength = buffer.Count;
                segment.Bytes(CollectionsMarshal.AsSpan(buffer));
                Drop();

                return (instOffset, instLength);
            }
        }
    }
}

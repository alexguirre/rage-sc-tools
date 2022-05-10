#nullable enable
namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using ScTools.GameFiles.Five;

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

        public Lexer Lexer { get; }
        public DiagnosticsReport Diagnostics { get; }
        public Script OutputScript { get; }
        public Dictionary<string, ConstantValue> Constants { get; }
        public Dictionary<string, Label> Labels { get; }
        public bool HasScriptName { get; private set; }
        public bool HasGlobalsSignature { get; private set; }
        public bool HasGlobalBlock { get; private set; }
        public NativeDB? NativeDB { get; set; }
        public AssemblerOptions Options { get; set; }

        public Assembler(Lexer lexer, DiagnosticsReport diagnostics)
        {
            Lexer = lexer;
            Diagnostics = diagnostics;
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
            var parser = new Parser(Lexer, Diagnostics);
            parser.ParseProgram().ForEach(ProcessLine);
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

        private void ProcessLine(Parser.Line line)
        {
            if (line.Label is not null)
            {
                ProcessLabel(line.Label);
            }

            if (line is Parser.Directive directive)
            {
                ProcessDirective(directive);
            }
            else if (line is Parser.Instruction instruction)
            {
                ProcessInstruction(instruction);
            }
        }

        private void ProcessLabel(Parser.Label label)
        {
            if (CurrentSegment == Segment.None)
            {
                Diagnostics.AddError($"Unexpected label outside any segment", label.Location);
                return;
            }

            var name = label.Name.Lexeme.ToString();
            var offset = CurrentSegmentBuilder.ByteLength / GetAddressingUnitByteSize(CurrentSegment);

            if (CurrentSegment == Segment.Global)
            {
                offset |= (int)(OutputScript.GlobalsBlock << 18);
            }

            if (Constants.ContainsKey(name))
            {
                Diagnostics.AddError($"Constant named '{name}' already defined", label.Location);
            }
            else if (!Labels.TryAdd(name, new Label(CurrentSegment, offset)))
            {
                Diagnostics.AddError($"Label '{name}' already defined", label.Location);
            }
        }

        private void ChangeSegment(string segmentName, SourceRange location)
        {
            CurrentSegment = segmentName switch
            {
                "global" => Segment.Global,
                "static" => Segment.Static,
                "arg" => Segment.Arg,
                "string" => Segment.String,
                "code" => Segment.Code,
                "include" => Segment.Include,
                _ => throw new InvalidOperationException(),
            };

            if (CurrentSegment == Segment.Global && !HasGlobalBlock)
            {
                Diagnostics.AddError($"Directive '.global_block' required before '.global' segment", location);
            }
        }

        private void ProcessDirective(Parser.Directive directive)
        {
            var name = directive.Name.Lexeme.ToString().ToLowerInvariant();
            switch (name)
            {
                // segments
                case "global":
                case "static":
                case "arg":
                case "string":
                case "code":
                case "include":
                    if (directive.Operands.Length != 0) { UnexpectedNumberOfOperandsError(directive, 0); }
                    
                    ChangeSegment(name, directive.Location);
                    break;

                case "script_name":
                    if (HasScriptName)
                    {
                        Diagnostics.AddError($"Directive '.script_name' is repeated", directive.Location);
                    }
                    else
                    {
                        var scriptName = Parser.MissingIdentifierLexeme;
                        if (directive.Operands.Length != 1)
                        {
                            UnexpectedNumberOfOperandsError(directive, 1);
                        }
                        else if (directive.Operands[0] is not Parser.DirectiveOperandIdentifier ident)
                        {
                            ExpectedIdentifierError(directive.Operands[0]);
                        }
                        else
                        {
                            scriptName = ident.Name.Lexeme.ToString();
                        }

                        OutputScript.Name = scriptName;
                        OutputScript.NameHash = scriptName.ToLowercaseHash();
                        HasScriptName = true;
                    }
                    break;

                case "globals_signature":
                    if (HasGlobalsSignature)
                    {
                        Diagnostics.AddError($"Directive '.globals_signature' is repeated", directive.Location);
                    }
                    else
                    {
                        var globalsSignature = 0u;
                        if (directive.Operands.Length != 1)
                        {
                            UnexpectedNumberOfOperandsError(directive, 1);
                        }
                        else if (directive.Operands[0] is not Parser.DirectiveOperandInteger integer)
                        {
                            ExpectedIntegerError(directive.Operands[0]);
                        }
                        else
                        {
                            globalsSignature = unchecked((uint)integer.Integer.GetIntLiteral());
                        }

                        OutputScript.GlobalsSignature = globalsSignature;
                        HasGlobalsSignature = true;
                    }
                    break;
                case "global_block":
                    if (HasGlobalBlock)
                    {
                        Diagnostics.AddError($"Directive '.global_block' is repeated", directive.Location);
                    }
                    else
                    {
                        var globalsBlock = 0xFFFFFFFF;
                        if (directive.Operands.Length != 1)
                        {
                            UnexpectedNumberOfOperandsError(directive, 1);
                        }
                        else if (directive.Operands[0] is not Parser.DirectiveOperandInteger integer)
                        {
                            ExpectedIntegerError(directive.Operands[0]);
                        }
                        else
                        {
                            globalsBlock = unchecked((uint)integer.Integer.GetIntLiteral());
                        }

                        OutputScript.GlobalsBlock = globalsBlock;
                        HasGlobalBlock = true;
                    }
                    break;
                case "const":
                    if (directive.Operands.Length != 2) { UnexpectedNumberOfOperandsError(directive, 2); }

                    var constName = Parser.MissingIdentifierLexeme;
                    var constNameLocation = SourceRange.Unknown;
                    if (directive.Operands.Length > 0)
                    {
                        if (directive.Operands[0] is Parser.DirectiveOperandIdentifier ident)
                        {
                            constName = ident.Name.Lexeme.ToString();
                            constNameLocation = ident.Name.Location;
                        }
                        else
                        {
                            ExpectedIdentifierError(directive.Operands[0]);
                        }
                    }

                    var isInteger = true;
                    var integerValue = 0L;
                    var floatValue = 0.0f;
                    if (directive.Operands.Length > 1)
                    {
                        if (directive.Operands[1] is Parser.DirectiveOperandInteger integer)
                        {
                            isInteger = true;
                            integerValue = integer.Integer.GetInt64Literal();
                        }
                        else if (directive.Operands[1] is Parser.DirectiveOperandFloat floatOp)
                        {
                            isInteger = false;
                            floatValue = floatOp.Float.GetFloatLiteral();
                        }
                        else
                        {
                            ExpectedIntegerOrFloatError(directive.Operands[1]);
                        }
                    }

                    var constValue = isInteger ?
                                        new ConstantValue(integerValue) :
                                        new ConstantValue(floatValue);

                    if (Labels.ContainsKey(constName))
                    {
                        Diagnostics.AddError($"Label named '{constName}' already defined", constNameLocation);
                    }
                    else if (!Constants.TryAdd(constName, constValue))
                    {
                        Diagnostics.AddError($"Constant '{constName}' already defined", constNameLocation);
                    }
                    break;
                case "int":
                    if (directive.Operands.Length == 0) { OneOrMoreOperandsRequiredError(directive); }
                    WriteIntFloatDirectiveOperands(directive.Operands, isFloat: false, isInt64: false);
                    break;
                case "int64":
                    if (directive.Operands.Length == 0) { OneOrMoreOperandsRequiredError(directive); }
                    WriteIntFloatDirectiveOperands(directive.Operands, isFloat: false, isInt64: true);
                    break;
                case "float":
                    if (directive.Operands.Length == 0) { OneOrMoreOperandsRequiredError(directive); }
                    WriteIntFloatDirectiveOperands(directive.Operands, isFloat: true, isInt64: false);
                    break;
                case "str":
                    var strToAdd = "";
                    if (directive.Operands.Length != 1)
                    { 
                        UnexpectedNumberOfOperandsError(directive, 1);
                    }
                    else if (directive.Operands[0] is not Parser.DirectiveOperandString str)
                    {
                        ExpectedStringError(directive.Operands[0]);
                    }
                    else
                    {
                        strToAdd = str.String.GetStringLiteral();
                    }
                    
                    CurrentSegmentBuilder.String(strToAdd);
                    break;
                case "native":
                    var hash = 0UL;
                    var hashLocation = SourceRange.Unknown;
                    if (directive.Operands.Length != 1)
                    {
                        UnexpectedNumberOfOperandsError(directive, 1);
                    }
                    else if (directive.Operands[0] is not Parser.DirectiveOperandInteger nativeHash)
                    {
                        ExpectedIntegerError(directive.Operands[0]);
                    }
                    else
                    {
                        hash = unchecked((ulong)nativeHash.Integer.GetInt64Literal());
                        hashLocation = nativeHash.Integer.Location;
                    }

                    if (NativeDB is not null && hash != 0)
                    {
                        var translatedHash = NativeDB.TranslateHash(hash, GameBuild.Latest);
                        if (translatedHash == 0)
                        {
                            Diagnostics.AddWarning($"Unknown native hash '{hash:X16}'", hashLocation);
                        }
                        else
                        {
                            hash = translatedHash;
                        }
                    }
                    CurrentSegmentBuilder.UInt64(hash);
                    break;
            }

            void ExpectedIdentifierError(Parser.DirectiveOperand operand)
                => Diagnostics.AddError($"Expected identifier but found {OperandToTypeName(operand)}", operand.Location);
            void ExpectedIntegerError(Parser.DirectiveOperand operand)
                => Diagnostics.AddError($"Expected integer but found {OperandToTypeName(operand)}", operand.Location);
            void ExpectedIntegerOrFloatError(Parser.DirectiveOperand operand)
                => Diagnostics.AddError($"Expected integer or float but found {OperandToTypeName(operand)}", operand.Location);
            void ExpectedStringError(Parser.DirectiveOperand operand)
                => Diagnostics.AddError($"Expected string but found {OperandToTypeName(operand)}", operand.Location);
            void UnexpectedNumberOfOperandsError(Parser.Directive directive, int expectedNumOperands)
                => Diagnostics.AddError($"Expected {expectedNumOperands} operands for directive '.{directive.Name.Lexeme}' but found {directive.Operands.Length} operands", directive.Location);
            void OneOrMoreOperandsRequiredError(Parser.Directive directive)
                => Diagnostics.AddError($"Expected at least 1 operand for directive '.{directive.Name.Lexeme}' but found none", directive.Location);

            static string OperandToTypeName(Parser.DirectiveOperand operand)
                => operand switch
                {
                    Parser.DirectiveOperandIdentifier _ => "identifier",
                    Parser.DirectiveOperandInteger _ => "integer",
                    Parser.DirectiveOperandFloat _ => "float",
                    Parser.DirectiveOperandString _ => "string",
                    Parser.DirectiveOperandDup _ => "'dup' operator",
                    _ => "unknown",
                };
        }

        private void ProcessInstruction(Parser.Instruction instruction)
        {
            if (CurrentSegment != Segment.Code)
            {
                Diagnostics.AddError($"Unexpected instruction in non-code segment", instruction.Location);
                return;
            }

            if (!Enum.TryParse<Opcode>(instruction.Opcode.Lexeme.Span, out var opcode))
            {
                Diagnostics.AddError($"Unknown opcode '{instruction.Opcode.Lexeme}'", instruction.Opcode.Location);
                return;
            }

            var expectedNumOperands = opcode.NumberOfOperands();
            var operands = instruction.Operands;
            if (expectedNumOperands != -1 && operands.Length != expectedNumOperands)
            {
                Diagnostics.AddError($"Expected {expectedNumOperands} operands for opcode {opcode} but found {operands.Length} operands", instruction.Location);
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
            instructions.Add(new(instruction, opcode, offset, length));
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
                    OperandToU8(span[0..], operands[0].A);
                    break;
                case Opcode.PUSH_CONST_U8_U8:
                case Opcode.LEAVE:
                    OperandToU8(span[0..], operands[0].A);
                    OperandToU8(span[1..], operands[1].A);
                    break;
                case Opcode.PUSH_CONST_U8_U8_U8:
                    OperandToU8(span[0..], operands[0].A);
                    OperandToU8(span[1..], operands[1].A);
                    OperandToU8(span[2..], operands[2].A);
                    break;
                case Opcode.PUSH_CONST_U32:
                    OperandToU32(span[0..], operands[0].A);
                    break;
                case Opcode.PUSH_CONST_F:
                    OperandToF32(span[0..], operands[0].A);
                    break;
                case Opcode.NATIVE:
                    var argCount = ParseOperandToUInt(operands[0].A);
                    var returnCount = ParseOperandToUInt(operands[1].A);
                    var nativeIndex = ParseOperandToUInt(operands[2].A);

                    CheckLossOfDataInUInt(argCount, maxBits: 6, Diagnostics, operands[0].A.Location);
                    CheckLossOfDataInUInt(returnCount, maxBits: 2, Diagnostics, operands[1].A.Location);
                    CheckLossOfDataInUInt(nativeIndex, maxBits: 16, Diagnostics, operands[2].A.Location);

                    span[0] = (byte)((argCount & 0x3F) << 2 | (returnCount & 0x3));
                    span[1] = (byte)((nativeIndex >> 8) & 0xFF);
                    span[2] = (byte)(nativeIndex & 0xFF);
                    break;
                case Opcode.ENTER:
                    OperandToU8(span[0..], operands[0].A);
                    OperandToU16(span[1..], operands[1].A);
                    // note: label name is already written in ProcessInstruction
                    break;
                case Opcode.PUSH_CONST_S16:
                case Opcode.IADD_S16:
                case Opcode.IMUL_S16:
                case Opcode.IOFFSET_S16:
                case Opcode.IOFFSET_S16_LOAD:
                case Opcode.IOFFSET_S16_STORE:
                    OperandToS16(span[0..], operands[0].A);
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
                    OperandToU16(span[0..], operands[0].A);
                    break;
                case Opcode.J:
                case Opcode.JZ:
                case Opcode.IEQ_JZ:
                case Opcode.INE_JZ:
                case Opcode.IGT_JZ:
                case Opcode.IGE_JZ:
                case Opcode.ILT_JZ:
                case Opcode.ILE_JZ:
                    OperandToRelativeLabelOffsetOrS16(span[0..], instruction.Offset + 1, operands[0].A);
                    break;
                case Opcode.CALL:
                case Opcode.GLOBAL_U24:
                case Opcode.GLOBAL_U24_LOAD:
                case Opcode.GLOBAL_U24_STORE:
                case Opcode.PUSH_CONST_U24:
                    OperandToU24(span[0..], operands[0].A);
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
                        if (operands[i].Type is Parser.InstructionOperandType.SwitchCase)
                        {
                            // TODO: warning if cases are repeated
                            OperandToU32(span[0..], operands[i].A);
                            OperandToRelativeLabelOffsetOrS16(span[4..], jumpToOperandOffset, operands[i].B);
                        }
                        else
                        {
                            Diagnostics.AddError("Expected switch-case operand", operands[i].Location);
                        }
                    }
                    break;
            }
        }

        private Span<byte> GetInstructionSpan(Instruction instruction)
            => codeSegmentBuilder.RawDataBuffer.Slice(instruction.Offset, instruction.Length);

        private ulong ParseOperandToUInt(Token operand)
        {
            long value = 0;
            switch (operand.Kind)
            {
                case TokenKind.Integer:
                    value = operand.GetInt64Literal();
                    break;
                case TokenKind.Float:
                    var floatValue = operand.GetFloatLiteral();
                    value = (long)Math.Truncate(floatValue);
                    Diagnostics.AddWarning("Floating-point number truncated", operand.Location);
                    break;
                case TokenKind.Identifier:
                    var name = operand.Lexeme.ToString();
                    if (Labels.TryGetValue(name, out var label))
                    {
                        value = label.Offset;
                    }
                    else if (Constants.TryGetValue(name, out var constValue))
                    {
                        if (constValue.DefinedAsFloat)
                        {
                            Diagnostics.AddWarning("Floating-point number truncated", operand.Location);
                        }

                        value = constValue.Integer;
                    }
                    else
                    {
                        Diagnostics.AddError($"'{name}' is undefined", operand.Location);
                    }
                    break;
            }

            if (value < 0)
            {
                Diagnostics.AddError("Found negative integer, expected unsigned integer", operand.Location);
            }

            return (ulong)value;
        }

        private long ParseOperandToInt(Token operand)
        {
            long value = 0;
            switch (operand.Kind)
            {
                case TokenKind.Integer:
                    value = operand.GetInt64Literal();
                    break;
                case TokenKind.Float:
                    var floatValue = operand.GetFloatLiteral();
                    value = (long)Math.Truncate(floatValue);
                    Diagnostics.AddWarning("Floating-point number truncated", operand.Location);
                    break;
                case TokenKind.Identifier:
                    var name = operand.Lexeme.ToString();
                    if (Labels.TryGetValue(name, out var label))
                    {
                        value = label.Offset;
                    }
                    else if (Constants.TryGetValue(name, out var constValue))
                    {
                        if (constValue.DefinedAsFloat)
                        {
                            Diagnostics.AddWarning("Floating-point number truncated", operand.Location);
                        }

                        value = constValue.Integer;
                    }
                    else
                    {
                        Diagnostics.AddError($"'{name}' is undefined", operand.Location);
                    }
                    break;
            }

            return value;
        }

        private float ParseOperandToFloat(Token operand)
        {
            float value = 0;
            switch (operand.Kind)
            {
                case TokenKind.Integer:
                    value = operand.GetFloatLiteral();
                    break;
                case TokenKind.Float:
                    value = operand.GetFloatLiteral();
                    break;
                case TokenKind.Identifier:
                    var name = operand.Lexeme.ToString();
                    if (Labels.TryGetValue(name, out _))
                    {
                        Diagnostics.AddError($"Expected floating-point number, cannot use label '{name}'", operand.Location);
                    }
                    else if (Constants.TryGetValue(name, out var constValue))
                    {
                        value = constValue.Float;
                    }
                    else
                    {
                        Diagnostics.AddError($"'{name}' is undefined", operand.Location);
                    }
                    break;
            }

            return value;
        }

        private void OperandToF32(Span<byte> dest, Token operand)
        {
            var value = ParseOperandToFloat(operand);

            MemoryMarshal.Write(dest, ref value);
        }

        private void OperandToU32(Span<byte> dest, Token operand)
        {
            var value = ParseOperandToUInt(operand);
            CheckLossOfDataInUInt(value, 32, Diagnostics, operand.Location);

            dest[0] = (byte)(value & 0xFF);
            dest[1] = (byte)((value >> 8) & 0xFF);
            dest[2] = (byte)((value >> 16) & 0xFF);
            dest[3] = (byte)((value >> 24) & 0xFF);
        }

        private void OperandToU24(Span<byte> dest, Token operand)
        {
            var value = ParseOperandToUInt(operand);
            CheckLossOfDataInUInt(value, 24, Diagnostics, operand.Location);

            dest[0] = (byte)(value & 0xFF);
            dest[1] = (byte)((value >> 8) & 0xFF);
            dest[2] = (byte)((value >> 16) & 0xFF);
        }

        private void OperandToU16(Span<byte> dest, Token operand)
        {
            var value = ParseOperandToUInt(operand);
            CheckLossOfDataInUInt(value, 16, Diagnostics, operand.Location);

            dest[0] = (byte)(value & 0xFF);
            dest[1] = (byte)((value >> 8) & 0xFF);
        }

        private void OperandToU8(Span<byte> dest, Token operand)
        {
            var value = ParseOperandToUInt(operand);
            CheckLossOfDataInUInt(value, 8, Diagnostics, operand.Location);

            dest[0] = (byte)(value & 0xFF);
        }

        private void OperandToS16(Span<byte> dest, Token operand)
        {
            var value = ParseOperandToInt(operand);
            CheckLossOfDataInInt(value, 16, Diagnostics, operand.Location);

            dest[0] = (byte)(value & 0xFF);
            dest[1] = (byte)((value >> 8) & 0xFF);
        }

        private void OperandToRelativeLabelOffsetOrS16(Span<byte> dest, int operandOffset, Token operand)
        {
            if (operand.Kind is TokenKind.Identifier && Labels.TryGetValue(operand.Lexeme.ToString(), out var label))
            {
                if (label.Segment != Segment.Code)
                {
                    Diagnostics.AddError($"Cannot jump to label '{operand.Lexeme}' outside code segment", operand.Location);
                    return;
                }

                var absOffset = label.Offset;
                var relOffset = absOffset - (operandOffset + 2);
                if (relOffset < short.MinValue || relOffset > short.MaxValue)
                {
                    Diagnostics.AddError($"Label '{operand.Lexeme}' is too far", operand.Location);
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

        private void WriteIntFloatDirectiveOperands(ImmutableArray<Parser.DirectiveOperand> operandList, bool isFloat, bool isInt64)
        {
            foreach (var operand in operandList)
            {
                switch (operand)
                {
                    case Parser.DirectiveOperandIdentifier identifierOperand:
                        if (TryGetConstant(identifierOperand.Name, out var constValue))
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
                    case Parser.DirectiveOperandInteger integerOperand:
                        var intValue = integerOperand.Integer.GetInt64Literal();
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
                    case Parser.DirectiveOperandFloat floatOperand:
                        var floatValue = floatOperand.Float.GetFloatLiteral();
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
                    case Parser.DirectiveOperandDup dupOperand:
                        long count = 0;
                        if (dupOperand.Count.Kind is TokenKind.Identifier && TryGetConstant(dupOperand.Count, out var countConst))
                        {
                            count = countConst.Integer;
                        }
                        else if (dupOperand.Count.Kind is TokenKind.Integer)
                        {
                            count = dupOperand.Count.GetInt64Literal();
                        }

                        for (long i = 0; i < count; i++)
                        {
                            WriteIntFloatDirectiveOperands(dupOperand.InnerOperands, isFloat, isInt64);
                        }
                        break;
                }
            }
        }

        private bool TryGetConstant(Token identifier, out ConstantValue value)
        {
            var name = identifier.Lexeme.ToString();
            if (Constants.TryGetValue(name, out value))
            {
                return true;
            }
            else
            {
                Diagnostics.AddError($"Undefined constant '{name}'", identifier.Location);
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
                nativeHashes[i] = Script.EncodeNativeHash(nativeHashes[i], i, codeLength);
            }
            return (nativeHashes, (uint)nativeHashes.Length);
        }

        public static Assembler Assemble(TextReader input, string filePath = "tmp.sc", NativeDB? nativeDB = null, AssemblerOptions options = default)
        {
            var d = new DiagnosticsReport();
            var a = new Assembler(new Lexer(filePath, input.ReadToEnd(), d), d) { NativeDB = nativeDB, Options = options };
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
            public ImmutableArray<Parser.InstructionOperand> Operands { get; }
            public SourceRange Source { get; }
            public Opcode Opcode { get; }
            public int Offset { get; }
            public int Length { get; }

            public Instruction(Parser.Instruction instruction, Opcode opcode, int offset, int length)
            {
                (Source, Opcode, Offset, Length) = (instruction.Location, opcode, offset, length);
                Operands = instruction.Operands;
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

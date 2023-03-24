namespace ScTools.ScriptAssembly.Targets.GTA4;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using ScTools;
using ScTools.GameFiles;
using ScTools.GameFiles.GTA4;
using ScTools.ScriptAssembly;

public partial class Assembler : IDisposable
{
    public enum Segment { None, Global, Static, Arg, Code }

    public static int GetSegmentAlignment(Segment segment) => segment switch
    {
        // ScriptValue-addressable
        Segment.Global or Segment.Static or Segment.Arg => Marshal.SizeOf<ScriptValue32>(),

        // byte-addressable
        _ => sizeof(byte),
    };


    private bool disposed;

    private readonly InstructionBuffer instBuffer;
    private readonly InstructionEmitter instEmitter;
    private readonly List<(InstructionReference Instruction, ImmutableArray<Parser.InstructionOperand> Operands)> instructionsToFix = new();
    private readonly HashSet<string> allLabelsSet = new();
    private readonly Dictionary<string, int>[] labelsBySegment;
    private readonly List<LabelInfo> codeLabels = new();

    private readonly SegmentBuilder globalSegmentBuilder = new(GetSegmentAlignment(Segment.Global), isPaged: false),
                                    staticSegmentBuilder = new(GetSegmentAlignment(Segment.Static), isPaged: false),
                                    argSegmentBuilder = new(GetSegmentAlignment(Segment.Arg), isPaged: false); // appended to the end of the static segment

    private Segment CurrentSegment { get; set; } = Segment.None;
    private SegmentBuilder CurrentSegmentBuilder => CurrentSegment switch
    {
        Segment.Global => globalSegmentBuilder,
        Segment.Static => staticSegmentBuilder,
        Segment.Arg => argSegmentBuilder,
        _ => throw new InvalidOperationException(),
    };

    public Lexer Lexer { get; }
    public DiagnosticsReport Diagnostics { get; }
    public Script OutputScript { get; }
    public bool HasGlobalsSignature { get; private set; }
    public NativeDB? NativeDB { get; set; }

    public Assembler(Lexer lexer, DiagnosticsReport diagnostics)
    {
        Lexer = lexer;
        Diagnostics = diagnostics;
        OutputScript = new();
        instBuffer = new();
        instEmitter = new(new AppendInstructionFlushStrategy(instBuffer));

        labelsBySegment = Enum.GetValues<Segment>().Select(s => new Dictionary<string, int>(CaseInsensitiveComparer)).ToArray();
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

        var code = instBuffer.Finish(codeLabels);
        OutputScript.Code = code;
        OutputScript.CodeLength = (uint)(code?.Length ?? 0);

        OutputScript.Globals = globalSegmentBuilder.Length != 0 ? MemoryMarshal.Cast<byte, ScriptValue32>(globalSegmentBuilder.RawDataBuffer).ToArray() : Array.Empty<ScriptValue32>();
        OutputScript.GlobalsCount = (uint)(OutputScript.Globals?.Length ?? 0);

        (OutputScript.Statics, OutputScript.StaticsCount, OutputScript.ArgsCount) = SegmentToStaticsArray(staticSegmentBuilder, argSegmentBuilder);
    }

    /// <summary>
    /// Traverses the parse tree. Allocates the space needed by each segment, initializes non-code segments, stores labels offsets
    /// and collects the instructions from the code segment.
    /// </summary>
    private void FirstPass()
    {
        var parser = new Parser(Lexer, Diagnostics, new(Diagnostics));
        parser.ParseProgram().ForEach(ProcessLine);
        FixArgLabels();
    }

    /// <summary>
    /// Assembles the instructions collected in the first pass, since now it knows the offsets of all labels.
    /// </summary>
    private void SecondPass()
    {
        foreach (var (inst, operands) in instructionsToFix)
        {
            FixInstruction(inst, operands);
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
        if (!allLabelsSet.Add(name))
        {
            Diagnostics.AddError($"Label '{name}' already defined", label.Location);
            return;
        }

        var labelsDict = labelsBySegment[(int)CurrentSegment];
        if (CurrentSegment is Segment.Code)
        {
            // the code labels will be resolved by the InstructionBuffer with codeLabels list,
            // so the label "offset" stores the code label index instead
            labelsDict.Add(name, codeLabels.Count);
            var labelMarker = instEmitter.EmitLabelMarker();
            codeLabels.Add(new(labelMarker, new()));
        }
        else
        {
            var offset = CurrentSegmentBuilder.ByteLength / GetSegmentAlignment(CurrentSegment);
            labelsDict.Add(name, offset);
        }
    }

    private void ChangeSegment(string segmentName, SourceRange location)
    {
        CurrentSegment = segmentName switch
        {
            "global" => Segment.Global,
            "static" => Segment.Static,
            "arg" => Segment.Arg,
            "code" => Segment.Code,
            _ => throw new InvalidOperationException(),
        };
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
            case "code":
                if (directive.Operands.Length != 0) { UnexpectedNumberOfOperandsError(directive, 0); }

                ChangeSegment(name, directive.Location);
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
        }

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

        var needsFixing = false;
        var u8 = (int operandIndex) => TryEarlyParseOperand(operands[operandIndex], ref needsFixing, ParseOperandToU8);
        var u16 = (int operandIndex) => TryEarlyParseOperand(operands[operandIndex], ref needsFixing, ParseOperandToU16);
        var u32 = (int operandIndex) => TryEarlyParseOperand(operands[operandIndex], ref needsFixing, ParseOperandToU32);
        var u32OrIdentifierHash = (int operandIndex) =>
        {
            Debug.Assert(operands[operandIndex].Type is not Parser.InstructionOperandType.SwitchCase);
            return ParseOperandToU32OrIdentifierHash(operands[operandIndex].A);
        };
        var f32 = (int operandIndex) => TryEarlyParseOperand(operands[operandIndex], ref needsFixing, ParseOperandToFloat);
        var switchOps = () =>
        {
            needsFixing = true;
            foreach (var operand in operands)
            {
                if (operand.Type is not Parser.InstructionOperandType.SwitchCase)
                {
                    Diagnostics.AddError($"Expected {Parser.InstructionOperandType.SwitchCase} operand but found {operand.Type}", operand.Location);
                }
            }
            return new (uint Value, uint Offset)[operands.Length];
        };
        var str = (int operandIndex) =>
        {
            var operand = operands[operandIndex];
            if (operand.Type is Parser.InstructionOperandType.String)
            {
                return operand.A.GetStringLiteral();
            }

            Diagnostics.AddError($"Expected {Parser.InstructionOperandType.String} operand but found {operand.Type}", operand.Location);
            return string.Empty;
        };

        var emittedInstruction = opcode switch
        {
            Opcode.IADD => instEmitter.EmitIAdd(),
            Opcode.ISUB => instEmitter.EmitISub(),
            Opcode.IMUL => instEmitter.EmitIMul(),
            Opcode.IDIV => instEmitter.EmitIDiv(),
            Opcode.IMOD => instEmitter.EmitIMod(),
            Opcode.INOT => instEmitter.EmitINot(),
            Opcode.INEG => instEmitter.EmitINeg(),
            Opcode.IEQ => instEmitter.EmitIEq(),
            Opcode.INE => instEmitter.EmitINe(),
            Opcode.IGT => instEmitter.EmitIGt(),
            Opcode.IGE => instEmitter.EmitIGe(),
            Opcode.ILT => instEmitter.EmitILt(),
            Opcode.ILE => instEmitter.EmitILe(),
            Opcode.FADD => instEmitter.EmitFAdd(),
            Opcode.FSUB => instEmitter.EmitFSub(),
            Opcode.FMUL => instEmitter.EmitFMul(),
            Opcode.FDIV => instEmitter.EmitFDiv(),
            Opcode.FMOD => instEmitter.EmitFMod(),
            Opcode.FNEG => instEmitter.EmitFNeg(),
            Opcode.FEQ => instEmitter.EmitFEq(),
            Opcode.FNE => instEmitter.EmitFNe(),
            Opcode.FGT => instEmitter.EmitFGt(),
            Opcode.FGE => instEmitter.EmitFGe(),
            Opcode.FLT => instEmitter.EmitFLt(),
            Opcode.FLE => instEmitter.EmitFLe(),
            Opcode.VADD => instEmitter.EmitVAdd(),
            Opcode.VSUB => instEmitter.EmitVSub(),
            Opcode.VMUL => instEmitter.EmitVMul(),
            Opcode.VDIV => instEmitter.EmitVDiv(),
            Opcode.VNEG => instEmitter.EmitVNeg(),
            Opcode.IAND => instEmitter.EmitIAnd(),
            Opcode.IOR => instEmitter.EmitIOr(),
            Opcode.IXOR => instEmitter.EmitIXor(),
            Opcode.I2F => instEmitter.EmitI2F(),
            Opcode.F2I => instEmitter.EmitF2I(),
            Opcode.F2V => instEmitter.EmitF2V(),
            Opcode.J => instEmitter.EmitJ(u32(0)),
            Opcode.JZ => instEmitter.EmitJZ(u32(0)),  
            Opcode.JNZ => instEmitter.EmitJNZ(u32(0)),
            Opcode.PUSH_CONST_U16 => instEmitter.EmitPushConstU16(u16(0)),
            Opcode.PUSH_CONST_U32 => instEmitter.EmitPushConstU32(u32(0)),
            Opcode.PUSH_CONST_F => instEmitter.EmitPushConstF(f32(0)),
            Opcode.DUP => instEmitter.EmitDup(),
            Opcode.DROP => instEmitter.EmitDrop(),
            Opcode.NATIVE => instEmitter.EmitNative(u8(0), u8(1), u32OrIdentifierHash(2)),
            Opcode.CALL => instEmitter.EmitCall(u32(0)),
            Opcode.ENTER => instEmitter.EmitEnter(u8(0), u16(1)),
            Opcode.LEAVE => instEmitter.EmitLeave(u8(0), u8(1)),
            Opcode.LOAD => instEmitter.EmitLoad(),
            Opcode.STORE => instEmitter.EmitStore(),
            Opcode.STORE_REV => instEmitter.EmitStoreRev(),
            Opcode.LOAD_N => instEmitter.EmitLoadN(),
            Opcode.STORE_N => instEmitter.EmitStoreN(),
            >= Opcode.LOCAL_0 and <= Opcode.LOCAL_7 => instEmitter.EmitLocalN((int)opcode - (int)Opcode.LOCAL_0),
            Opcode.LOCAL => instEmitter.EmitLocal(),
            Opcode.STATIC => instEmitter.EmitStatic(),
            Opcode.GLOBAL => instEmitter.EmitGlobal(),
            Opcode.ARRAY => instEmitter.EmitArray(),
            Opcode.SWITCH => instEmitter.EmitSwitch(switchOps()),
            Opcode.STRING => instEmitter.EmitString(str(0)),
            Opcode.NULL => instEmitter.EmitNull(),
            Opcode.TEXT_LABEL_ASSIGN_STRING => instEmitter.EmitTextLabelAssignString(u8(0)),
            Opcode.TEXT_LABEL_ASSIGN_INT => instEmitter.EmitTextLabelAssignInt(u8(0)),
            Opcode.TEXT_LABEL_APPEND_STRING => instEmitter.EmitTextLabelAppendString(u8(0)),
            Opcode.TEXT_LABEL_APPEND_INT => instEmitter.EmitTextLabelAppendInt(u8(0)),
            Opcode.CATCH => instEmitter.EmitCatch(),
            Opcode.THROW => instEmitter.EmitThrow(),
            Opcode.TEXT_LABEL_COPY => instEmitter.EmitTextLabelCopy(),
            Opcode._XPROTECT_LOAD => instEmitter.EmitXProtectLoad(),
            Opcode._XPROTECT_STORE => instEmitter.EmitXProtectStore(),
            Opcode._XPROTECT_REF => instEmitter.EmitXProtectRef(),
            >= Opcode.PUSH_CONST_M16 and <= Opcode.PUSH_CONST_159 => instEmitter.EmitPushConstN((int)opcode - (int)Opcode.PUSH_CONST_0),
            _ => throw new InvalidOperationException($"Unknown opcode '{opcode}'"),
        };

        if (needsFixing)
        {
            instructionsToFix.Add((emittedInstruction, operands));
        }
    }

    static TInt TryEarlyParseOperand<TInt>(Parser.InstructionOperand operand, ref bool needsFixing, Func<Token, TInt> parser) where TInt : struct
    {
        Debug.Assert(operand.Type is not Parser.InstructionOperandType.SwitchCase);
        if (operand.Type is Parser.InstructionOperandType.Identifier)
        {
            // labels will be fixed in the second pass
            needsFixing = true;
            return default;
        }
        return parser(operand.A);
    }

    private void FixInstruction(InstructionReference instruction, ImmutableArray<Parser.InstructionOperand> operands)
    {
        instEmitter.FlushStrategy = new UpdateInstructionFlushStrategy(instBuffer, instruction);

        uint p(Token operand, int operandOffset, int numberOfBytesToFill, bool noCodeLabels = false) => operand.Kind is TokenKind.Identifier ?
                                        unchecked((uint)FixLabelOperand(instruction, operand, operandOffset, numberOfBytesToFill, noCodeLabels)) :
                                        ParseOperandToU32(operand);

        var u8 = (int operandIndex, int operandOffset) => unchecked((byte)p(operands[operandIndex].A, operandOffset, numberOfBytesToFill: 1));
        var u16 = (int operandIndex, int operandOffset) => unchecked((ushort)p(operands[operandIndex].A, operandOffset, numberOfBytesToFill: 2));
        var u32 = (int operandIndex, int operandOffset) => unchecked((uint)p(operands[operandIndex].A, operandOffset, numberOfBytesToFill: 4));
        var u32OrIdentifierHash = (int operandIndex) =>
        {
            Debug.Assert(operands[operandIndex].Type is not Parser.InstructionOperandType.SwitchCase);
            return ParseOperandToU32OrIdentifierHash(operands[operandIndex].A);
        };
        var switchOps = () =>
        {
            var cases = new (uint Value, uint RelativeOffset)[operands.Length];
            int i = 0;
            foreach (var operand in operands)
            {
                if (operand.Type is Parser.InstructionOperandType.SwitchCase)
                {
                    var caseOffset = 2 + i * 8;
                    var caseValue = p(operand.A, caseOffset, numberOfBytesToFill: 4);
                    var caseJumpOffset = p(operand.B, caseOffset + 4, numberOfBytesToFill: 4);
                    cases[i] = (caseValue, caseJumpOffset);
                }
                i++;
            }
            return cases;
        };
        
        var opcode = instBuffer.GetOpcode(instruction);
        _ = opcode switch
        {
            Opcode.J => instEmitter.EmitJ(u32(0, 1)),
            Opcode.JZ => instEmitter.EmitJZ(u32(0, 1)),  
            Opcode.JNZ => instEmitter.EmitJNZ(u32(0, 1)),
            Opcode.PUSH_CONST_U16 => instEmitter.EmitPushConstU16(u16(0, 1)),
            Opcode.PUSH_CONST_U32 => instEmitter.EmitPushConstU32(u32(0, 1)),
            Opcode.NATIVE => instEmitter.EmitNative(u8(0, 1), u8(1, 2), u32OrIdentifierHash(2)),
            Opcode.CALL => instEmitter.EmitCall(u32(0, 1)),
            Opcode.ENTER => instEmitter.EmitEnter(u8(0, 1), u16(1, 2)),
            Opcode.LEAVE => instEmitter.EmitLeave(u8(0, 1), u8(1, 2)),
            Opcode.SWITCH => instEmitter.EmitSwitch(switchOps()),
            Opcode.TEXT_LABEL_ASSIGN_STRING => instEmitter.EmitTextLabelAssignString(u8(0, 1)),
            Opcode.TEXT_LABEL_ASSIGN_INT => instEmitter.EmitTextLabelAssignInt(u8(0, 1)),
            Opcode.TEXT_LABEL_APPEND_STRING => instEmitter.EmitTextLabelAppendString(u8(0, 1)),
            Opcode.TEXT_LABEL_APPEND_INT => instEmitter.EmitTextLabelAppendInt(u8(0, 1)),
            _ => throw new InvalidOperationException($"Opcode '{opcode}' shouldn't need fixing"),
        };
    }

    private int FixLabelOperand(InstructionReference instruction, Token operand, int operandOffset, int numberOfBytesToFill, bool noCodeLabels = false)
    {
        var name = operand.Lexeme.ToString();
        if (TryGetLabelOffset(name, out var labelOffset, out var labelSegment))
        {
            if (labelSegment is Segment.Code)
            {
                if (noCodeLabels)
                {
                    Diagnostics.AddError($"Cannot use code label reference here", operand.Location);
                    return 0;
                }

                Debug.Assert(numberOfBytesToFill == 4, "Not enough or too many bytes for a code label reference");
                ReferenceCodeLabel(operand, instruction, operandOffset);
                return 0; // will be filled in later in InstructionBuffer.Finish
            }
            else
            {
                return labelOffset;
            }
        }
        else
        {
            Diagnostics.AddError($"Label '{name}' is undefined", operand.Location);
        }

        return 0;
    }

    private bool TryGetLabelOffset(string name, out int labelOffset, out Segment segment)
    {
        int i = 0;
        foreach (var labelDict in labelsBySegment)
        {
            if (labelDict.TryGetValue(name, out labelOffset))
            {
                segment = (Segment)i;
                return true;
            }
            i++;
        }

        labelOffset = 0;
        segment = Segment.None;
        return false;
    }

    private ulong ParseOperandToU64(Token operand)
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
            //case TokenKind.Identifier when segmentForLabels is Segment.None:
            //    Diagnostics.AddError($"Cannot use labels for this operand", operand.Location);
            //    break;
            //case TokenKind.Identifier when segmentForLabels is Segment.Code:
            //    throw new InvalidOperationException("Cannot get code label address during assembly");
            case TokenKind.Identifier:
                var name = operand.Lexeme.ToString();
                if (TryGetLabelOffset(name, out var labelOffset, out var labelSegment))
                {
                    value = labelOffset;
                }
                else
                {
                    Diagnostics.AddError($"Label '{name}' is undefined", operand.Location);
                }
                break;
        }

        if (value < 0)
        {
            Diagnostics.AddError("Found negative integer, expected unsigned integer", operand.Location);
        }

        return (ulong)value;
    }

    private uint ParseOperandToU32(Token operand)
    {
        var value = ParseOperandToU64(operand);
        CheckLossOfDataInUInt(value, 32, Diagnostics, operand.Location);
        return unchecked((uint)(value & 0xFFFFFFFF));
    }

    private uint ParseOperandToU32OrIdentifierHash(Token operand)
    {
        if (operand.Kind == TokenKind.Identifier)
        {
            return operand.Lexeme.ToLowercaseHash();
        }
        else
        {
            return ParseOperandToU32(operand);
        }
    }

    private uint ParseOperandToU24(Token operand)
    {
        var value = ParseOperandToU64(operand);
        CheckLossOfDataInUInt(value, 24, Diagnostics, operand.Location);
        return unchecked((uint)(value & 0x00FFFFFF));
    }

    private ushort ParseOperandToU16(Token operand)
    {
        var value = ParseOperandToU64(operand);
        CheckLossOfDataInUInt(value, 16, Diagnostics, operand.Location);
        return unchecked((ushort)(value & 0x0000FFFF));
    }

    private byte ParseOperandToU8(Token operand)
    {
        var value = ParseOperandToU64(operand);
        CheckLossOfDataInUInt(value, 8, Diagnostics, operand.Location);
        return unchecked((byte)(value & 0x000000FF));
    }

    private long ParseOperandToS64(Token operand)
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
            //case TokenKind.Identifier when segmentForLabels is Segment.None:
            //    Diagnostics.AddError($"Cannot use labels for this operand", operand.Location);
            //    break;
            //case TokenKind.Identifier when segmentForLabels is Segment.Code:
            //    throw new InvalidOperationException("Cannot get code label address during assembly");
            case TokenKind.Identifier:
                var name = operand.Lexeme.ToString();
                if (TryGetLabelOffset(name, out var labelOffset, out var labelSegment))
                {
                    value = labelOffset;
                }
                else
                {
                    Diagnostics.AddError($"Label '{name}' is undefined", operand.Location);
                }
                break;
        }

        return value;
    }

    private short ParseOperandToS16(Token operand)
    {
        var value = ParseOperandToS64(operand);
        CheckLossOfDataInInt(value, 16, Diagnostics, operand.Location);
        return unchecked((short)(value & 0x0000FFFF));
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
                Diagnostics.AddError($"Expected floating-point number, cannot use label '{name}'", operand.Location);
                break;
        }

        return value;
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
        var maxValue = (1L << maxBits - 1) - 1;
        var minValue = -(1L << maxBits - 1);

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
                    long count = dupOperand.Count.GetInt64Literal();

                    for (long i = 0; i < count; i++)
                    {
                        WriteIntFloatDirectiveOperands(dupOperand.InnerOperands, isFloat, isInt64);
                    }
                    break;
            }
        }
    }

    private void ReferenceCodeLabel(Token label, InstructionReference instruction, int operandOffset)
    {
        var name = label.Lexeme.ToString();
        if (!labelsBySegment[(int)Segment.Code].TryGetValue(name, out var codeLabelIndex))
        {
            Diagnostics.AddError($"Label '{name}' is undefined", label.Location);
            return;
        }

        codeLabels[codeLabelIndex].UnresolvedReferences.Add(new(instruction, operandOffset));
    }

    /// <summary>
    /// The args are stored after the static variables, so add the static segment length to the offset of labels in the '.arg' segment and
    /// add them to the '.static' segment labels dictionary.
    /// </summary>
    private void FixArgLabels()
    {
        var staticSegmentLength = staticSegmentBuilder.ByteLength / GetSegmentAlignment(Segment.Static);
        var staticLabels = labelsBySegment[(int)Segment.Static];
        var argLabels = labelsBySegment[(int)Segment.Arg];
        foreach (var (name, offset) in argLabels)
        {
            staticLabels[name] = staticSegmentLength + offset;
        }
    }

    private static (ScriptValue32[] Statics, uint StaticsCount, uint ArgsCount) SegmentToStaticsArray(SegmentBuilder staticSegment, SegmentBuilder argSegment)
    {
        var statics = MemoryMarshal.Cast<byte, ScriptValue32>(staticSegment.RawDataBuffer);
        var args = MemoryMarshal.Cast<byte, ScriptValue32>(argSegment.RawDataBuffer);

        var combined = new ScriptValue32[statics.Length + args.Length];
        statics.CopyTo(combined.AsSpan(0, statics.Length));
        args.CopyTo(combined.AsSpan(statics.Length, args.Length));
        return (combined, (uint)combined.Length, (uint)args.Length);
    }

    public static Assembler Assemble(TextReader input, string filePath = "tmp.scasm", NativeDB? nativeDB = null)
    {
        var d = new DiagnosticsReport();
        var a = new Assembler(new Lexer(filePath, input.ReadToEnd(), d), d) { NativeDB = nativeDB };
        a.Assemble();
        return a;
    }

    public static StringComparer CaseInsensitiveComparer => StringComparer.OrdinalIgnoreCase;
}

namespace ScTools.ScriptAssembly.Targets.GTA5;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using ScTools;
using ScTools.GameFiles;
using GameFiles.GTA5;
using ScTools.ScriptAssembly;

public readonly struct AssemblerOptions
{
    /// <summary>
    /// If <c>true</c>, the ENTER instructions will be encoded including the closest label declared before it.
    /// </summary>
    public bool IncludeFunctionNames { get; init; }
}

public partial class Assembler : IDisposable
{
    public const string DefaultScriptName = "unknown";

    public enum Segment { None, Global, Static, Arg, String, Code, Include }

    public static int GetSegmentAlignment(Segment segment) => segment switch
    {
        // ScriptValue-addressable
        Segment.Global or Segment.Static or Segment.Arg => Marshal.SizeOf<ScriptValue64>(),

        // NativeHash-addressable
        Segment.Include => sizeof(ulong),

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

    private readonly SegmentBuilder globalSegmentBuilder = new(GetSegmentAlignment(Segment.Global), isPaged: true),
                                    staticSegmentBuilder = new(GetSegmentAlignment(Segment.Static), isPaged: false),
                                    argSegmentBuilder = new(GetSegmentAlignment(Segment.Arg), isPaged: false), // appended to the end of the static segment
                                    stringSegmentBuilder = new(GetSegmentAlignment(Segment.String), isPaged: true),
                                    includeSegmentBuilder = new(GetSegmentAlignment(Segment.Include), isPaged: false);

    private Segment CurrentSegment { get; set; } = Segment.None;
    private SegmentBuilder CurrentSegmentBuilder => CurrentSegment switch
    {
        Segment.Global => globalSegmentBuilder,
        Segment.Static => staticSegmentBuilder,
        Segment.Arg => argSegmentBuilder,
        Segment.String => stringSegmentBuilder,
        Segment.Include => includeSegmentBuilder,
        _ => throw new InvalidOperationException(),
    };

    public Lexer Lexer { get; }
    public DiagnosticsReport Diagnostics { get; }
    public ScTools.GameFiles.GTA5.Script OutputScript { get; }
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
                stringSegmentBuilder.Dispose();
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

        var codePages = instBuffer.Finish(codeLabels);
        OutputScript.CodePages = codePages;
        OutputScript.CodeLength = codePages?.Length ?? 0;

        OutputScript.GlobalsPages = globalSegmentBuilder.Length != 0 ? globalSegmentBuilder.ToPages<ScriptValue64>() : null;
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
            if (CurrentSegment == Segment.Global)
            {
                offset |= (int)(OutputScript.GlobalsBlock << 18);
            }

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
                    else if (directive.Operands[0] is not Parser.DirectiveOperandString nameOperand)
                    {
                        ExpectedStringError(directive.Operands[0]);
                    }
                    else
                    {
                        scriptName = nameOperand.String.GetStringLiteral();
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

        if (!Enum.TryParse<OpcodeV10>(instruction.Opcode.Lexeme.Span, out var opcode))
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
        var u24 = (int operandIndex) => TryEarlyParseOperand(operands[operandIndex], ref needsFixing, ParseOperandToU24);
        var u32 = (int operandIndex) => TryEarlyParseOperand(operands[operandIndex], ref needsFixing, ParseOperandToU32);
        var s16 = (int operandIndex) => TryEarlyParseOperand(operands[operandIndex], ref needsFixing, ParseOperandToS16);
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
            return new (uint Value, short RelativeOffset)[operands.Length];
        };

        var emittedInstruction = opcode switch
        {
            OpcodeV10.NOP => instEmitter.EmitNop(),
            OpcodeV10.IADD => instEmitter.EmitIAdd(),
            OpcodeV10.ISUB => instEmitter.EmitISub(),
            OpcodeV10.IMUL => instEmitter.EmitIMul(),
            OpcodeV10.IDIV => instEmitter.EmitIDiv(),
            OpcodeV10.IMOD => instEmitter.EmitIMod(),
            OpcodeV10.INOT => instEmitter.EmitINot(),
            OpcodeV10.INEG => instEmitter.EmitINeg(),
            OpcodeV10.IEQ => instEmitter.EmitIEq(),
            OpcodeV10.INE => instEmitter.EmitINe(),
            OpcodeV10.IGT => instEmitter.EmitIGt(),
            OpcodeV10.IGE => instEmitter.EmitIGe(),
            OpcodeV10.ILT => instEmitter.EmitILt(),
            OpcodeV10.ILE => instEmitter.EmitILe(),
            OpcodeV10.FADD => instEmitter.EmitFAdd(),
            OpcodeV10.FSUB => instEmitter.EmitFSub(),
            OpcodeV10.FMUL => instEmitter.EmitFMul(),
            OpcodeV10.FDIV => instEmitter.EmitFDiv(),
            OpcodeV10.FMOD => instEmitter.EmitFMod(),
            OpcodeV10.FNEG => instEmitter.EmitFNeg(),
            OpcodeV10.FEQ => instEmitter.EmitFEq(),
            OpcodeV10.FNE => instEmitter.EmitFNe(),
            OpcodeV10.FGT => instEmitter.EmitFGt(),
            OpcodeV10.FGE => instEmitter.EmitFGe(),
            OpcodeV10.FLT => instEmitter.EmitFLt(),
            OpcodeV10.FLE => instEmitter.EmitFLe(),
            OpcodeV10.VADD => instEmitter.EmitVAdd(),
            OpcodeV10.VSUB => instEmitter.EmitVSub(),
            OpcodeV10.VMUL => instEmitter.EmitVMul(),
            OpcodeV10.VDIV => instEmitter.EmitVDiv(),
            OpcodeV10.VNEG => instEmitter.EmitVNeg(),
            OpcodeV10.IAND => instEmitter.EmitIAnd(),
            OpcodeV10.IOR => instEmitter.EmitIOr(),
            OpcodeV10.IXOR => instEmitter.EmitIXor(),
            OpcodeV10.I2F => instEmitter.EmitI2F(),
            OpcodeV10.F2I => instEmitter.EmitF2I(),
            OpcodeV10.F2V => instEmitter.EmitF2V(),
            OpcodeV10.PUSH_CONST_U8 => instEmitter.EmitPushConstU8(u8(0)),
            OpcodeV10.PUSH_CONST_U8_U8 => instEmitter.EmitPushConstU8U8(u8(0), u8(1)),
            OpcodeV10.PUSH_CONST_U8_U8_U8 => instEmitter.EmitPushConstU8U8U8(u8(0), u8(1), u8(2)),
            OpcodeV10.PUSH_CONST_U32 => instEmitter.EmitPushConstU32(u32(0)),
            OpcodeV10.PUSH_CONST_F => instEmitter.EmitPushConstF(f32(0)),
            OpcodeV10.DUP => instEmitter.EmitDup(),
            OpcodeV10.DROP => instEmitter.EmitDrop(),
            OpcodeV10.NATIVE => instEmitter.EmitNative(u8(0), u8(1), u16(2)),
            OpcodeV10.ENTER => instEmitter.EmitEnter(u8(0), u16(1), GetFunctionName()),
            OpcodeV10.LEAVE => instEmitter.EmitLeave(u8(0), u8(1)),
            OpcodeV10.LOAD => instEmitter.EmitLoad(),
            OpcodeV10.STORE => instEmitter.EmitStore(),
            OpcodeV10.STORE_REV => instEmitter.EmitStoreRev(),
            OpcodeV10.LOAD_N => instEmitter.EmitLoadN(),
            OpcodeV10.STORE_N => instEmitter.EmitStoreN(),
            OpcodeV10.ARRAY_U8 => instEmitter.EmitArrayU8(u8(0)),
            OpcodeV10.ARRAY_U8_LOAD => instEmitter.EmitArrayU8Load(u8(0)),
            OpcodeV10.ARRAY_U8_STORE => instEmitter.EmitArrayU8Store(u8(0)),
            OpcodeV10.LOCAL_U8 => instEmitter.EmitLocalU8(u8(0)),
            OpcodeV10.LOCAL_U8_LOAD => instEmitter.EmitLocalU8Load(u8(0)),
            OpcodeV10.LOCAL_U8_STORE => instEmitter.EmitLocalU8Store(u8(0)),
            OpcodeV10.STATIC_U8 => instEmitter.EmitStaticU8(u8(0)),
            OpcodeV10.STATIC_U8_LOAD => instEmitter.EmitStaticU8Load(u8(0)),
            OpcodeV10.STATIC_U8_STORE => instEmitter.EmitStaticU8Store(u8(0)),
            OpcodeV10.IADD_U8 => instEmitter.EmitIAddU8(u8(0)),
            OpcodeV10.IMUL_U8 => instEmitter.EmitIMulU8(u8(0)),
            OpcodeV10.IOFFSET => instEmitter.EmitIOffset(),
            OpcodeV10.IOFFSET_U8 => instEmitter.EmitIOffsetU8(u8(0)),
            OpcodeV10.IOFFSET_U8_LOAD => instEmitter.EmitIOffsetU8Load(u8(0)),
            OpcodeV10.IOFFSET_U8_STORE => instEmitter.EmitIOffsetU8Store(u8(0)),
            OpcodeV10.PUSH_CONST_S16 => instEmitter.EmitPushConstS16(s16(0)),
            OpcodeV10.IADD_S16 => instEmitter.EmitIAddS16(s16(0)),
            OpcodeV10.IMUL_S16 => instEmitter.EmitIMulS16(s16(0)),
            OpcodeV10.IOFFSET_S16 => instEmitter.EmitIOffsetS16(s16(0)),
            OpcodeV10.IOFFSET_S16_LOAD => instEmitter.EmitIOffsetS16Load(s16(0)),
            OpcodeV10.IOFFSET_S16_STORE => instEmitter.EmitIOffsetS16Store(s16(0)),
            OpcodeV10.ARRAY_U16 => instEmitter.EmitArrayU16(u16(0)),
            OpcodeV10.ARRAY_U16_LOAD => instEmitter.EmitArrayU16Load(u16(0)),
            OpcodeV10.ARRAY_U16_STORE => instEmitter.EmitArrayU16Store(u16(0)),
            OpcodeV10.LOCAL_U16 => instEmitter.EmitLocalU16(u16(0)),
            OpcodeV10.LOCAL_U16_LOAD => instEmitter.EmitLocalU16Load(u16(0)),
            OpcodeV10.LOCAL_U16_STORE => instEmitter.EmitLocalU16Store(u16(0)),
            OpcodeV10.STATIC_U16 => instEmitter.EmitStaticU16(u16(0)),
            OpcodeV10.STATIC_U16_LOAD => instEmitter.EmitStaticU16Load(u16(0)),
            OpcodeV10.STATIC_U16_STORE => instEmitter.EmitStaticU16Store(u16(0)),
            OpcodeV10.GLOBAL_U16 => instEmitter.EmitGlobalU16(u16(0)),
            OpcodeV10.GLOBAL_U16_LOAD => instEmitter.EmitGlobalU16Load(u16(0)),
            OpcodeV10.GLOBAL_U16_STORE => instEmitter.EmitGlobalU16Store(u16(0)),
            OpcodeV10.J => instEmitter.EmitJ(s16(0)),
            OpcodeV10.JZ => instEmitter.EmitJZ(s16(0)),
            OpcodeV10.IEQ_JZ => instEmitter.EmitIEqJZ(s16(0)),
            OpcodeV10.INE_JZ => instEmitter.EmitINeJZ(s16(0)),
            OpcodeV10.IGT_JZ => instEmitter.EmitIGtJZ(s16(0)),
            OpcodeV10.IGE_JZ => instEmitter.EmitIGeJZ(s16(0)),
            OpcodeV10.ILT_JZ => instEmitter.EmitILtJZ(s16(0)),
            OpcodeV10.ILE_JZ => instEmitter.EmitILeJZ(s16(0)),
            OpcodeV10.CALL => instEmitter.EmitCall(u24(0)),
            OpcodeV10.GLOBAL_U24 => instEmitter.EmitGlobalU24(u24(0)),
            OpcodeV10.GLOBAL_U24_LOAD => instEmitter.EmitGlobalU24Load(u24(0)),
            OpcodeV10.GLOBAL_U24_STORE => instEmitter.EmitGlobalU24Store(u24(0)),
            OpcodeV10.PUSH_CONST_U24 => instEmitter.EmitPushConstU24(u24(0)),
            OpcodeV10.SWITCH => instEmitter.EmitSwitch(switchOps()),
            OpcodeV10.STRING => instEmitter.EmitString(),
            OpcodeV10.STRINGHASH => instEmitter.EmitStringHash(),
            OpcodeV10.TEXT_LABEL_ASSIGN_STRING => instEmitter.EmitTextLabelAssignString(u8(0)),
            OpcodeV10.TEXT_LABEL_ASSIGN_INT => instEmitter.EmitTextLabelAssignInt(u8(0)),
            OpcodeV10.TEXT_LABEL_APPEND_STRING => instEmitter.EmitTextLabelAppendString(u8(0)),
            OpcodeV10.TEXT_LABEL_APPEND_INT => instEmitter.EmitTextLabelAppendInt(u8(0)),
            OpcodeV10.TEXT_LABEL_COPY => instEmitter.EmitTextLabelCopy(),
            OpcodeV10.CATCH => instEmitter.EmitCatch(),
            OpcodeV10.THROW => instEmitter.EmitThrow(),
            OpcodeV10.CALLINDIRECT => instEmitter.EmitCallIndirect(),
            OpcodeV10.PUSH_CONST_M1 => instEmitter.EmitPushConstM1(),
            OpcodeV10.PUSH_CONST_0 => instEmitter.EmitPushConst0(),
            OpcodeV10.PUSH_CONST_1 => instEmitter.EmitPushConst1(),
            OpcodeV10.PUSH_CONST_2 => instEmitter.EmitPushConst2(),
            OpcodeV10.PUSH_CONST_3 => instEmitter.EmitPushConst3(),
            OpcodeV10.PUSH_CONST_4 => instEmitter.EmitPushConst4(),
            OpcodeV10.PUSH_CONST_5 => instEmitter.EmitPushConst5(),
            OpcodeV10.PUSH_CONST_6 => instEmitter.EmitPushConst6(),
            OpcodeV10.PUSH_CONST_7 => instEmitter.EmitPushConst7(),
            OpcodeV10.PUSH_CONST_FM1 => instEmitter.EmitPushConstFM1(),
            OpcodeV10.PUSH_CONST_F0 => instEmitter.EmitPushConstF0(),
            OpcodeV10.PUSH_CONST_F1 => instEmitter.EmitPushConstF1(),
            OpcodeV10.PUSH_CONST_F2 => instEmitter.EmitPushConstF2(),
            OpcodeV10.PUSH_CONST_F3 => instEmitter.EmitPushConstF3(),
            OpcodeV10.PUSH_CONST_F4 => instEmitter.EmitPushConstF4(),
            OpcodeV10.PUSH_CONST_F5 => instEmitter.EmitPushConstF5(),
            OpcodeV10.PUSH_CONST_F6 => instEmitter.EmitPushConstF6(),
            OpcodeV10.PUSH_CONST_F7 => instEmitter.EmitPushConstF7(),
            OpcodeV10.IBITTEST => throw new NotImplementedException(nameof(OpcodeV10.IBITTEST)),
            _ => throw new InvalidOperationException($"Unknown opcode '{opcode}'"),
        };

        if (needsFixing)
        {
            instructionsToFix.Add((emittedInstruction, operands));
        }

        string? GetFunctionName()
        {
            var labelInst = instBuffer.GetRef(instBuffer.NumberOfInstructions - 1);
            if (instBuffer.GetLength(labelInst) != 0)
            {
                // is not label marker
                return null;
            }
            
            var labelIndex = codeLabels.FindIndex(l => l.Instruction == labelInst);
            return labelIndex == -1 ? null : labelsBySegment[(int)Segment.Code].Where(kvp => kvp.Value == labelIndex).First().Key;
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

        // TODO: continue here
        uint p(Token operand, int operandOffset, int numberOfBytesToFill, bool noCodeLabels = false) => operand.Kind is TokenKind.Identifier ?
                                        unchecked((uint)FixLabelOperand(instruction, operand, operandOffset, numberOfBytesToFill, noCodeLabels)) :
                                        ParseOperandToU32(operand);

        var u8 = (int operandIndex, int operandOffset) => unchecked((byte)p(operands[operandIndex].A, operandOffset, numberOfBytesToFill: 1));
        var u8NoCodeLabels = (int operandIndex) => unchecked((byte)p(operands[operandIndex].A, 0, numberOfBytesToFill: 1, noCodeLabels: true));
        var u16 = (int operandIndex, int operandOffset) => unchecked((ushort)p(operands[operandIndex].A, operandOffset, numberOfBytesToFill: 2));
        var u16NoCodeLabels = (int operandIndex) => unchecked((ushort)p(operands[operandIndex].A, 0, numberOfBytesToFill: 2, noCodeLabels: true));
        var u24 = (int operandIndex, int operandOffset) => unchecked((uint)p(operands[operandIndex].A, operandOffset, numberOfBytesToFill: 3));
        var u32 = (int operandIndex, int operandOffset) => unchecked((uint)p(operands[operandIndex].A, operandOffset, numberOfBytesToFill: 4));
        var s16 = (int operandIndex, int operandOffset) => unchecked((short)p(operands[operandIndex].A, operandOffset, numberOfBytesToFill: 2));
        var switchOps = () =>
        {
            var cases = new (uint Value, short RelativeOffset)[operands.Length];
            int i = 0;
            foreach (var operand in operands)
            {
                if (operand.Type is Parser.InstructionOperandType.SwitchCase)
                {
                    var caseOffset = 2 + i * 6;
                    var caseValue = p(operand.A, caseOffset, numberOfBytesToFill: 4);
                    var caseJumpOffset = unchecked((short)p(operand.B, caseOffset + 4, numberOfBytesToFill: 2));
                    cases[i] = (caseValue, caseJumpOffset);
                }
                i++;
            }
            return cases;
        };
        
        var opcode = instBuffer.GetOpcode(instruction);
        _ = opcode switch
        {
            OpcodeV10.PUSH_CONST_U8 => instEmitter.EmitPushConstU8(u8(0, 1)),
            OpcodeV10.PUSH_CONST_U8_U8 => instEmitter.EmitPushConstU8U8(u8(0, 1), u8(1, 2)),
            OpcodeV10.PUSH_CONST_U8_U8_U8 => instEmitter.EmitPushConstU8U8U8(u8(0, 1), u8(1, 2), u8(2, 3)),
            OpcodeV10.PUSH_CONST_U32 => instEmitter.EmitPushConstU32(u32(0, 1)),
            OpcodeV10.NATIVE => instEmitter.EmitNative(u8NoCodeLabels(0), u8NoCodeLabels(1), u16NoCodeLabels(2)),
            OpcodeV10.ENTER => instEmitter.EmitEnter(u8(0, 1), u16(1, 2), opcode.GetEnterFunctionName(CollectionsMarshal.AsSpan(instBuffer.GetBytes(instruction)))),
            OpcodeV10.LEAVE => instEmitter.EmitLeave(u8(0, 1), u8(1, 1)),
            OpcodeV10.ARRAY_U8 => instEmitter.EmitArrayU8(u8(0, 1)),
            OpcodeV10.ARRAY_U8_LOAD => instEmitter.EmitArrayU8Load(u8(0, 1)),
            OpcodeV10.ARRAY_U8_STORE => instEmitter.EmitArrayU8Store(u8(0, 1)),
            OpcodeV10.LOCAL_U8 => instEmitter.EmitLocalU8(u8(0, 1)),
            OpcodeV10.LOCAL_U8_LOAD => instEmitter.EmitLocalU8Load(u8(0, 1)),
            OpcodeV10.LOCAL_U8_STORE => instEmitter.EmitLocalU8Store(u8(0, 1)),
            OpcodeV10.STATIC_U8 => instEmitter.EmitStaticU8(u8(0, 1)),
            OpcodeV10.STATIC_U8_LOAD => instEmitter.EmitStaticU8Load(u8(0, 1)),
            OpcodeV10.STATIC_U8_STORE => instEmitter.EmitStaticU8Store(u8(0, 1)),
            OpcodeV10.IADD_U8 => instEmitter.EmitIAddU8(u8(0, 1)),
            OpcodeV10.IMUL_U8 => instEmitter.EmitIMulU8(u8(0, 1)),
            OpcodeV10.IOFFSET_U8 => instEmitter.EmitIOffsetU8(u8(0, 1)),
            OpcodeV10.IOFFSET_U8_LOAD => instEmitter.EmitIOffsetU8Load(u8(0, 1)),
            OpcodeV10.IOFFSET_U8_STORE => instEmitter.EmitIOffsetU8Store(u8(0, 1)),
            OpcodeV10.PUSH_CONST_S16 => instEmitter.EmitPushConstS16(s16(0, 1)),
            OpcodeV10.IADD_S16 => instEmitter.EmitIAddS16(s16(0, 1)),
            OpcodeV10.IMUL_S16 => instEmitter.EmitIMulS16(s16(0, 1)),
            OpcodeV10.IOFFSET_S16 => instEmitter.EmitIOffsetS16(s16(0, 1)),
            OpcodeV10.IOFFSET_S16_LOAD => instEmitter.EmitIOffsetS16Load(s16(0, 1)),
            OpcodeV10.IOFFSET_S16_STORE => instEmitter.EmitIOffsetS16Store(s16(0, 1)),
            OpcodeV10.ARRAY_U16 => instEmitter.EmitArrayU16(u16(0, 1)),
            OpcodeV10.ARRAY_U16_LOAD => instEmitter.EmitArrayU16Load(u16(0, 1)),
            OpcodeV10.ARRAY_U16_STORE => instEmitter.EmitArrayU16Store(u16(0, 1)),
            OpcodeV10.LOCAL_U16 => instEmitter.EmitLocalU16(u16(0, 1)),
            OpcodeV10.LOCAL_U16_LOAD => instEmitter.EmitLocalU16Load(u16(0, 1)),
            OpcodeV10.LOCAL_U16_STORE => instEmitter.EmitLocalU16Store(u16(0, 1)),
            OpcodeV10.STATIC_U16 => instEmitter.EmitStaticU16(u16(0, 1)),
            OpcodeV10.STATIC_U16_LOAD => instEmitter.EmitStaticU16Load(u16(0, 1)),
            OpcodeV10.STATIC_U16_STORE => instEmitter.EmitStaticU16Store(u16(0, 1)),
            OpcodeV10.GLOBAL_U16 => instEmitter.EmitGlobalU16(u16(0, 1)),
            OpcodeV10.GLOBAL_U16_LOAD => instEmitter.EmitGlobalU16Load(u16(0, 1)),
            OpcodeV10.GLOBAL_U16_STORE => instEmitter.EmitGlobalU16Store(u16(0, 1)),
            OpcodeV10.J => instEmitter.EmitJ(s16(0, 1)),
            OpcodeV10.JZ => instEmitter.EmitJZ(s16(0, 1)),
            OpcodeV10.IEQ_JZ => instEmitter.EmitIEqJZ(s16(0, 1)),
            OpcodeV10.INE_JZ => instEmitter.EmitINeJZ(s16(0, 1)),
            OpcodeV10.IGT_JZ => instEmitter.EmitIGtJZ(s16(0, 1)),
            OpcodeV10.IGE_JZ => instEmitter.EmitIGeJZ(s16(0, 1)),
            OpcodeV10.ILT_JZ => instEmitter.EmitILtJZ(s16(0, 1)),
            OpcodeV10.ILE_JZ => instEmitter.EmitILeJZ(s16(0, 1)),
            OpcodeV10.CALL => instEmitter.EmitCall(u24(0, 1)),
            OpcodeV10.GLOBAL_U24 => instEmitter.EmitGlobalU24(u24(0, 1)),
            OpcodeV10.GLOBAL_U24_LOAD => instEmitter.EmitGlobalU24Load(u24(0, 1)),
            OpcodeV10.GLOBAL_U24_STORE => instEmitter.EmitGlobalU24Store(u24(0, 1)),
            OpcodeV10.PUSH_CONST_U24 => instEmitter.EmitPushConstU24(u24(0, 1)),
            OpcodeV10.SWITCH => instEmitter.EmitSwitch(switchOps()),
            OpcodeV10.TEXT_LABEL_ASSIGN_STRING => instEmitter.EmitTextLabelAssignString(u8(0, 1)),
            OpcodeV10.TEXT_LABEL_ASSIGN_INT => instEmitter.EmitTextLabelAssignInt(u8(0, 1)),
            OpcodeV10.TEXT_LABEL_APPEND_STRING => instEmitter.EmitTextLabelAppendString(u8(0, 1)),
            OpcodeV10.TEXT_LABEL_APPEND_INT => instEmitter.EmitTextLabelAppendInt(u8(0, 1)),
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

                LabelReferenceKind? labelReferenceKind = null;
                if (numberOfBytesToFill < 2)
                {
                    Diagnostics.AddError($"Not enough bytes for a code label reference", operand.Location);
                }
                else if (numberOfBytesToFill == 2)
                {
                    labelReferenceKind = LabelReferenceKind.Relative;
                }
                else if (numberOfBytesToFill == 3)
                {
                    labelReferenceKind = LabelReferenceKind.Absolute;
                }
                else
                {
                    Diagnostics.AddError($"Too many bytes for a code label reference", operand.Location);
                }

                if (labelReferenceKind is not null)
                {
                    ReferenceCodeLabel(operand, instruction, operandOffset, labelReferenceKind.Value);
                }
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

    private void ReferenceCodeLabel(Token label, InstructionReference instruction, int operandOffset, LabelReferenceKind kind)
    {
        var name = label.Lexeme.ToString();
        if (!labelsBySegment[(int)Segment.Code].TryGetValue(name, out var codeLabelIndex))
        {
            Diagnostics.AddError($"Label '{name}' is undefined", label.Location);
            return;
        }

        codeLabels[codeLabelIndex].UnresolvedReferences.Add(new(instruction, operandOffset, kind));
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

    private static (ScriptValue64[] Statics, uint StaticsCount, uint ArgsCount) SegmentToStaticsArray(SegmentBuilder staticSegment, SegmentBuilder argSegment)
    {
        var statics = MemoryMarshal.Cast<byte, ScriptValue64>(staticSegment.RawDataBuffer);
        var args = MemoryMarshal.Cast<byte, ScriptValue64>(argSegment.RawDataBuffer);

        var combined = new ScriptValue64[statics.Length + args.Length];
        statics.CopyTo(combined.AsSpan(0, statics.Length));
        args.CopyTo(combined.AsSpan(statics.Length, args.Length));
        return (combined, (uint)combined.Length, (uint)args.Length);
    }

    private static (ulong[] Natives, uint NativesCount) SegmentToNativesArray(SegmentBuilder segment, uint codeLength)
    {
        var nativeHashes = MemoryMarshal.Cast<byte, ulong>(segment.RawDataBuffer).ToArray();
        for (int i = 0; i < nativeHashes.Length; i++)
        {
            nativeHashes[i] = ScTools.GameFiles.GTA5.Script.EncodeNativeHash(nativeHashes[i], i, codeLength);
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
        public OpcodeV10 Opcode { get; }
        public int Offset { get; }
        public int Length { get; }

        public Instruction(Parser.Instruction instruction, OpcodeV10 opcode, int offset, int length)
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
            buffer.Add((byte)(v >> 8 & 0xFF));
            buffer.Add((byte)(v >> 16 & 0xFF));
            buffer.Add((byte)(v >> 24));
        }

        public void U24(uint v)
        {
            buffer.Add((byte)(v & 0xFF));
            buffer.Add((byte)(v >> 8 & 0xFF));
            buffer.Add((byte)(v >> 16 & 0xFF));
        }

        public unsafe void F32(float v) => U32(*(uint*)&v);

        public void Opcode(OpcodeV10 v) => U8((byte)v);

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
            int offset = (int)(segment.Length & ScTools.GameFiles.GTA5.Script.MaxPageLength - 1);

            OpcodeV10 opcode = (OpcodeV10)buffer[0];

            // At page boundary a NOP may be required for the interpreter to switch to the next page,
            // the interpreter only does this with control flow instructions and NOP
            // If the NOP is needed, skip 1 byte at the end of the page
            bool needsNopAtBoundary = !opcode.IsControlFlow() &&
                                      opcode != OpcodeV10.NOP;

            if (offset + buffer.Count > ScTools.GameFiles.GTA5.Script.MaxPageLength - (needsNopAtBoundary ? 1u : 0)) // the instruction doesn't fit in the current page
            {
                var bytesUntilNextPage = (int)ScTools.GameFiles.GTA5.Script.MaxPageLength - offset; // padding needed to skip to the next page
                var requiredNops = bytesUntilNextPage;

                const int JumpInstructionSize = 3;
                if (bytesUntilNextPage > JumpInstructionSize)
                {
                    // if there is enough space for a J instruction, add it to jump to the next page
                    short relIP = (short)(ScTools.GameFiles.GTA5.Script.MaxPageLength - (offset + JumpInstructionSize)); // get how many bytes until the next page
                    segment.Byte((byte)OpcodeV10.J);
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

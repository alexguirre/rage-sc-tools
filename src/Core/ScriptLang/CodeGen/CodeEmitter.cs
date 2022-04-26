namespace ScTools.ScriptLang.CodeGen;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using ScTools.GameFiles;
using ScTools.ScriptAssembly;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

public sealed class CodeEmitter
{
    private readonly StatementEmitter stmtEmitter;
    private readonly ValueEmitter valueEmitter;
    private readonly AddressEmitter addressEmitter;
    //private readonly PatternOptimizer optimizer;
    private const bool IncludeFunctionNames = true;

    //private readonly SegmentBuilder codeSegment = new(sizeof(byte), isPaged: true);
    private readonly List<byte> codeBuffer = new(capacity: (int)Script.MaxPageLength);
    private readonly List<int> codeInstructionOffsets = new(capacity: (int)Script.MaxPageLength / 4);
    private readonly List<byte> instructionBuffer = new();

    private readonly HashSet<FunctionDeclaration> usedFunctions = new();
    private readonly Queue<FunctionDeclaration> functionsToCompile = new();
    private readonly HashSet<VarDeclaration> usedStatics = new();
    private readonly List<VarDeclaration> statics = new();
    private readonly Dictionary<VarDeclaration, int> staticsOffsets = new();

    private byte currentFunctionArgCount;
    private int currentFunctionFrameSize = 0;
    private TypeInfo? currentFunctionReturnType = null;
    private readonly Dictionary<VarDeclaration, int> currentFunctionAllocatedLocals = new();

    public StringsTable Strings { get; } = new();

    public CodeEmitter()
    {
        stmtEmitter = new(this);
        valueEmitter = new(this);
        addressEmitter = new(this);
    }

    public ScriptPageArray<byte> ToCodePages() => throw new NotImplementedException(nameof(ToCodePages));//codeSegment.ToPages<byte>();

    #region Byte Emitters
    private void EmitBytes(ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes)
        {
            instructionBuffer.Add(b);
        }
    }

    private void EmitU8(byte v)
    {
        instructionBuffer.Add(v);
    }

    private void EmitU16(ushort v)
    {
        instructionBuffer.Add((byte)(v & 0xFF));
        instructionBuffer.Add((byte)(v >> 8));
    }

    private void EmitS16(short v) => EmitU16(unchecked((ushort)v));

    private void EmitU32(uint v)
    {
        instructionBuffer.Add((byte)(v & 0xFF));
        instructionBuffer.Add((byte)((v >> 8) & 0xFF));
        instructionBuffer.Add((byte)((v >> 16) & 0xFF));
        instructionBuffer.Add((byte)(v >> 24));
    }

    private void EmitU24(uint v)
    {
        Debug.Assert((v & 0xFFFFFF) == v);
        instructionBuffer.Add((byte)(v & 0xFF));
        instructionBuffer.Add((byte)((v >> 8) & 0xFF));
        instructionBuffer.Add((byte)((v >> 16) & 0xFF));
    }

    private unsafe void EmitF32(float v) => EmitU32(*(uint*)&v);

    private void EmitOpcode(Opcode v) => EmitU8((byte)v);

    /// <summary>
    /// Clears the current instruction buffer.
    /// </summary>
    private void Drop()
    {
        instructionBuffer.Clear();
    }

    private record struct FlushResult(int InstructionIndex, int InstructionLength);

    /// <summary>
    /// Writes the current instruction buffer to the segment.
    /// </summary>
    private FlushResult Flush()
    {
        //int offset = (int)(codeSegment.Length & (Script.MaxPageLength - 1));

        //Opcode opcode = (Opcode)instructionBuffer[0];

        //// At page boundary a NOP may be required for the interpreter to switch to the next page,
        //// the interpreter only does this with control flow instructions and NOP
        //// If the NOP is needed, skip 1 byte at the end of the page
        //bool needsNopAtBoundary = !opcode.IsControlFlow() &&
        //                          opcode != Opcode.NOP;

        //if (offset + instructionBuffer.Count > (Script.MaxPageLength - (needsNopAtBoundary ? 1u : 0))) // the instruction doesn't fit in the current page
        //{
        //    var bytesUntilNextPage = (int)Script.MaxPageLength - offset; // padding needed to skip to the next page
        //    var requiredNops = bytesUntilNextPage;

        //    const int JumpInstructionSize = 3;
        //    if (bytesUntilNextPage > JumpInstructionSize)
        //    {
        //        // if there is enough space for a J instruction, add it to jump to the next page
        //        short relIP = (short)(Script.MaxPageLength - (offset + JumpInstructionSize)); // get how many bytes until the next page
        //        codeSegment.Byte((byte)Opcode.J);
        //        codeSegment.Byte((byte)(relIP & 0xFF));
        //        codeSegment.Byte((byte)(relIP >> 8));
        //        requiredNops -= JumpInstructionSize;
        //    }

        //    // NOP what is left of the current page
        //    codeSegment.Bytes(new byte[requiredNops]);
        //}

        //var instOffset = codeSegment.Length;
        //var instLength = instructionBuffer.Count;
        //codeSegment.Bytes(CollectionsMarshal.AsSpan(instructionBuffer));
        //Drop();

        var instIndex = codeInstructionOffsets.Count;
        var instLength = instructionBuffer.Count;
        codeInstructionOffsets.Add(codeBuffer.Count);
        codeBuffer.AddRange(instructionBuffer);
        Drop();

        return new(instIndex, instLength);
    }
    #endregion Byte Emitters

    #region Instruction Emitters
    private FlushResult EmitInst(Opcode opcode)
    {
        EmitOpcode(opcode);
        return Flush();
    }
    private FlushResult EmitInstU8(Opcode opcode, byte operand)
    {
        EmitOpcode(opcode);
        EmitU8(operand);
        return Flush();
    }
    private FlushResult EmitInstU8U8(Opcode opcode, byte operand1, byte operand2)
    {
        EmitOpcode(opcode);
        EmitU8(operand1);
        EmitU8(operand2);
        return Flush();
    }
    private FlushResult EmitInstU8U8U8(Opcode opcode, byte operand1, byte operand2, byte operand3)
    {
        EmitOpcode(opcode);
        EmitU8(operand1);
        EmitU8(operand2);
        EmitU8(operand3);
        return Flush();
    }
    private FlushResult EmitInstS16(Opcode opcode, short operand)
    {
        EmitOpcode(opcode);
        EmitS16(operand);
        return Flush();
    }
    private FlushResult EmitInstU16(Opcode opcode, ushort operand)
    {
        EmitOpcode(opcode);
        EmitU16(operand);
        return Flush();
    }
    private FlushResult EmitInstU24(Opcode opcode, uint operand)
    {
        EmitOpcode(opcode);
        EmitU24(operand);
        return Flush();
    }
    private FlushResult EmitInstU32(Opcode opcode, uint operand)
    {
        EmitOpcode(opcode);
        EmitU32(operand);
        return Flush();
    }
    private FlushResult EmitInstF32(Opcode opcode, float operand)
    {
        EmitOpcode(opcode);
        EmitF32(operand);
        return Flush();
    }

    private FlushResult EmitNop() => EmitInst(Opcode.NOP);
    private FlushResult EmitIAdd() => EmitInst(Opcode.IADD);
    private FlushResult EmitIAddU8(byte value) => EmitInstU8(Opcode.IADD_U8, value);
    private FlushResult EmitIAddS16(short value) => EmitInstS16(Opcode.IADD_S16, value);
    private FlushResult EmitISub() => EmitInst(Opcode.ISUB);
    private FlushResult EmitIMul() => EmitInst(Opcode.IMUL);
    private FlushResult EmitIMulU8(byte value) => EmitInstU8(Opcode.IMUL_U8, value);
    private FlushResult EmitIMulS16(short value) => EmitInstS16(Opcode.IMUL_S16, value);
    private FlushResult EmitIDiv() => EmitInst(Opcode.IDIV);
    private FlushResult EmitIMod() => EmitInst(Opcode.IMOD);
    private FlushResult EmitINot() => EmitInst(Opcode.INOT);
    private FlushResult EmitINeg() => EmitInst(Opcode.INEG);
    private FlushResult EmitIEq() => EmitInst(Opcode.IEQ);
    private FlushResult EmitINe() => EmitInst(Opcode.INE);
    private FlushResult EmitIGt() => EmitInst(Opcode.IGT);
    private FlushResult EmitIGe() => EmitInst(Opcode.IGE);
    private FlushResult EmitILt() => EmitInst(Opcode.ILT);
    private FlushResult EmitILe() => EmitInst(Opcode.ILE);
    private FlushResult EmitFAdd() => EmitInst(Opcode.FADD);
    private FlushResult EmitFSub() => EmitInst(Opcode.FSUB);
    private FlushResult EmitFMul() => EmitInst(Opcode.FMUL);
    private FlushResult EmitFDiv() => EmitInst(Opcode.FDIV);
    private FlushResult EmitFMod() => EmitInst(Opcode.FMOD);
    private FlushResult EmitFNeg() => EmitInst(Opcode.FNEG);
    private FlushResult EmitFEq() => EmitInst(Opcode.FEQ);
    private FlushResult EmitFNe() => EmitInst(Opcode.FNE);
    private FlushResult EmitFGt() => EmitInst(Opcode.FGT);
    private FlushResult EmitFGe() => EmitInst(Opcode.FGE);
    private FlushResult EmitFLt() => EmitInst(Opcode.FLT);
    private FlushResult EmitFLe() => EmitInst(Opcode.FLE);
    private FlushResult EmitVAdd() => EmitInst(Opcode.VADD);
    private FlushResult EmitVSub() => EmitInst(Opcode.VSUB);
    private FlushResult EmitVMul() => EmitInst(Opcode.VMUL);
    private FlushResult EmitVDiv() => EmitInst(Opcode.VDIV);
    private FlushResult EmitIAnd() => EmitInst(Opcode.IAND);
    private FlushResult EmitIOr() => EmitInst(Opcode.IOR);
    private FlushResult EmitIXor() => EmitInst(Opcode.IXOR);
    private FlushResult EmitI2F() => EmitInst(Opcode.I2F);
    private FlushResult EmitF2I() => EmitInst(Opcode.F2I);
    private FlushResult EmitF2V() => EmitInst(Opcode.F2V);
    private FlushResult EmitDup() => EmitInst(Opcode.DUP);
    private FlushResult EmitDrop() => EmitInst(Opcode.DROP);
    private FlushResult EmitLoad() => EmitInst(Opcode.LOAD);
    private FlushResult EmitLoadN() => EmitInst(Opcode.LOAD_N);
    private FlushResult EmitStore() => EmitInst(Opcode.STORE);
    private FlushResult EmitStoreN() => EmitInst(Opcode.STORE_N);
    private FlushResult EmitStoreRev() => EmitInst(Opcode.STORE_REV);
    private FlushResult EmitString() => EmitInst(Opcode.STRING);
    private FlushResult EmitStringHash() => EmitInst(Opcode.STRINGHASH);
    private FlushResult EmitPushConstM1() => EmitInst(Opcode.PUSH_CONST_M1);
    private FlushResult EmitPushConst0() => EmitInst(Opcode.PUSH_CONST_0);
    private FlushResult EmitPushConst1() => EmitInst(Opcode.PUSH_CONST_1);
    private FlushResult EmitPushConst2() => EmitInst(Opcode.PUSH_CONST_2);
    private FlushResult EmitPushConst3() => EmitInst(Opcode.PUSH_CONST_3);
    private FlushResult EmitPushConst4() => EmitInst(Opcode.PUSH_CONST_4);
    private FlushResult EmitPushConst5() => EmitInst(Opcode.PUSH_CONST_5);
    private FlushResult EmitPushConst6() => EmitInst(Opcode.PUSH_CONST_6);
    private FlushResult EmitPushConst7() => EmitInst(Opcode.PUSH_CONST_7);
    private FlushResult EmitPushConstFM1() => EmitInst(Opcode.PUSH_CONST_FM1);
    private FlushResult EmitPushConstF0() => EmitInst(Opcode.PUSH_CONST_F0);
    private FlushResult EmitPushConstF1() => EmitInst(Opcode.PUSH_CONST_F1);
    private FlushResult EmitPushConstF2() => EmitInst(Opcode.PUSH_CONST_F2);
    private FlushResult EmitPushConstF3() => EmitInst(Opcode.PUSH_CONST_F3);
    private FlushResult EmitPushConstF4() => EmitInst(Opcode.PUSH_CONST_F4);
    private FlushResult EmitPushConstF5() => EmitInst(Opcode.PUSH_CONST_F5);
    private FlushResult EmitPushConstF6() => EmitInst(Opcode.PUSH_CONST_F6);
    private FlushResult EmitPushConstF7() => EmitInst(Opcode.PUSH_CONST_F7);
    private FlushResult EmitPushConstU8(byte value) => EmitInstU8(Opcode.PUSH_CONST_U8, value);
    private FlushResult EmitPushConstU8U8(byte value1, byte value2) => EmitInstU8U8(Opcode.PUSH_CONST_U8_U8, value1, value2);
    private FlushResult EmitPushConstU8U8U8(byte value1, byte value2, byte value3) => EmitInstU8U8U8(Opcode.PUSH_CONST_U8_U8_U8, value1, value2, value3);
    private FlushResult EmitPushConstS16(short value) => EmitInstS16(Opcode.PUSH_CONST_S16, value);
    private FlushResult EmitPushConstU24(uint value) => EmitInstU24(Opcode.PUSH_CONST_U24, value);
    private FlushResult EmitPushConstU32(uint value) => EmitInstU32(Opcode.PUSH_CONST_U32, value);
    private FlushResult EmitPushConstF(float value) => EmitInstF32(Opcode.PUSH_CONST_F, value);
    private FlushResult EmitArrayU8(byte itemSize) => EmitInstU8(Opcode.ARRAY_U8, itemSize);
    private FlushResult EmitArrayU8Load(byte itemSize) => EmitInstU8(Opcode.ARRAY_U8_LOAD, itemSize);
    private FlushResult EmitArrayU8Store(byte itemSize) => EmitInstU8(Opcode.ARRAY_U8_STORE, itemSize);
    private FlushResult EmitArrayU16(ushort itemSize) => EmitInstU16(Opcode.ARRAY_U16, itemSize);
    private FlushResult EmitArrayU16Load(ushort itemSize) => EmitInstU16(Opcode.ARRAY_U16_LOAD, itemSize);
    private FlushResult EmitArrayU16Store(ushort itemSize) => EmitInstU16(Opcode.ARRAY_U16_STORE, itemSize);
    private FlushResult EmitLocalU8(byte frameOffset) => EmitInstU8(Opcode.LOCAL_U8, frameOffset);
    private FlushResult EmitLocalU8Load(byte frameOffset) => EmitInstU8(Opcode.LOCAL_U8_LOAD, frameOffset);
    private FlushResult EmitLocalU8Store(byte frameOffset) => EmitInstU8(Opcode.LOCAL_U8_STORE, frameOffset);
    private FlushResult EmitLocalU16(ushort frameOffset) => EmitInstU16(Opcode.LOCAL_U16, frameOffset);
    private FlushResult EmitLocalU16Load(ushort frameOffset) => EmitInstU16(Opcode.LOCAL_U16_LOAD, frameOffset);
    private FlushResult EmitLocalU16Store(ushort frameOffset) => EmitInstU16(Opcode.LOCAL_U16_STORE, frameOffset);
    private FlushResult EmitStaticU8(byte staticOffset) => EmitInstU8(Opcode.STATIC_U8, staticOffset);
    private FlushResult EmitStaticU8Load(byte staticOffset) => EmitInstU8(Opcode.STATIC_U8_LOAD, staticOffset);
    private FlushResult EmitStaticU8Store(byte staticOffset) => EmitInstU8(Opcode.STATIC_U8_STORE, staticOffset);
    private FlushResult EmitStaticU16(ushort staticOffset) => EmitInstU16(Opcode.STATIC_U16, staticOffset);
    private FlushResult EmitStaticU16Load(ushort staticOffset) => EmitInstU16(Opcode.STATIC_U16_LOAD, staticOffset);
    private FlushResult EmitStaticU16Store(ushort staticOffset) => EmitInstU16(Opcode.STATIC_U16_STORE, staticOffset);
    private FlushResult EmitGlobalU16(ushort globalOffset) => EmitInstU16(Opcode.GLOBAL_U16, globalOffset);
    private FlushResult EmitGlobalU16Load(ushort globalOffset) => EmitInstU16(Opcode.GLOBAL_U16_LOAD, globalOffset);
    private FlushResult EmitGlobalU16Store(ushort globalOffset) => EmitInstU16(Opcode.GLOBAL_U16_STORE, globalOffset);
    private FlushResult EmitGlobalU24(uint globalOffset) => EmitInstU24(Opcode.GLOBAL_U24, globalOffset);
    private FlushResult EmitGlobalU24Load(uint globalOffset) => EmitInstU24(Opcode.GLOBAL_U24_LOAD, globalOffset);
    private FlushResult EmitGlobalU24Store(uint globalOffset) => EmitInstU24(Opcode.GLOBAL_U24_STORE, globalOffset);
    private FlushResult EmitIOffset() => EmitInst(Opcode.IOFFSET);
    private FlushResult EmitIOffsetU8(byte offset) => EmitInstU8(Opcode.IOFFSET_U8, offset);
    private FlushResult EmitIOffsetU8Load(byte offset) => EmitInstU8(Opcode.IOFFSET_U8_LOAD, offset);
    private FlushResult EmitIOffsetU8Store(byte offset) => EmitInstU8(Opcode.IOFFSET_U8_STORE, offset);
    private FlushResult EmitIOffsetS16(short offset) => EmitInstS16(Opcode.IOFFSET_S16, offset);
    private FlushResult EmitIOffsetS16Load(short offset) => EmitInstS16(Opcode.IOFFSET_S16_LOAD, offset);
    private FlushResult EmitIOffsetS16Store(short offset) => EmitInstS16(Opcode.IOFFSET_S16_STORE, offset);
    private FlushResult EmitTextLabelAssignString(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_ASSIGN_STRING, textLabelLength);
    private FlushResult EmitTextLabelAssignInt(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_ASSIGN_INT, textLabelLength);
    private FlushResult EmitTextLabelAppendString(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_APPEND_STRING, textLabelLength);
    private FlushResult EmitTextLabelAppendInt(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_APPEND_INT, textLabelLength);
    private FlushResult EmitTextLabelCopy() => EmitInst(Opcode.TEXT_LABEL_COPY);
    private FlushResult EmitNative(byte argCount, byte returnCount, ushort nativeIndex)
    {
        Debug.Assert((argCount & 0x3F) == argCount); // arg count max bits 6
        Debug.Assert((returnCount & 0x3) == returnCount); // arg count max bits 2
        return EmitInstU8U8U8(Opcode.NATIVE,
            operand1: (byte)((argCount & 0x3F) << 2 | (returnCount & 0x3)),
            operand2: (byte)((nativeIndex >> 8) & 0xFF),
            operand3: (byte)(nativeIndex & 0xFF));
    }
    private FlushResult EmitEnter(byte argCount, ushort frameSize, string? name)
    {
        EmitOpcode(Opcode.ENTER);
        EmitU8(argCount);
        EmitU16(frameSize);
        if (IncludeFunctionNames && name is not null)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name).AsSpan();
            nameBytes = nameBytes[..Math.Min(nameBytes.Length, byte.MaxValue - 1)]; // limit length to 255 bytes (including null terminators)
            EmitU8((byte)(nameBytes.Length + 1));
            EmitBytes(nameBytes);
            EmitU8(0); // null terminator
        }
        else
        {
            EmitU8(0);
        }
        return Flush();
    }
    private FlushResult EmitLeave(byte argCount, byte returnCount) => EmitInstU8U8(Opcode.LEAVE, argCount, returnCount);
    private FlushResult EmitJ(short relativeOffset) => EmitInstS16(Opcode.J, relativeOffset);
    private FlushResult EmitJZ(short relativeOffset) => EmitInstS16(Opcode.JZ, relativeOffset);
    private FlushResult EmitIEqJZ(short relativeOffset) => EmitInstS16(Opcode.IEQ_JZ, relativeOffset);
    private FlushResult EmitINeJZ(short relativeOffset) => EmitInstS16(Opcode.INE_JZ, relativeOffset);
    private FlushResult EmitIGtJZ(short relativeOffset) => EmitInstS16(Opcode.IGT_JZ, relativeOffset);
    private FlushResult EmitIGeJZ(short relativeOffset) => EmitInstS16(Opcode.IGE_JZ, relativeOffset);
    private FlushResult EmitILtJZ(short relativeOffset) => EmitInstS16(Opcode.ILT_JZ, relativeOffset);
    private FlushResult EmitILeJZ(short relativeOffset) => EmitInstS16(Opcode.ILE_JZ, relativeOffset);
    private FlushResult EmitCall(uint functionOffset) => EmitInstU24(Opcode.CALL, functionOffset);
    private FlushResult EmitCallIndirect() => EmitInst(Opcode.CALLINDIRECT);
    private FlushResult EmitSwitch(byte numCases) // cases will be backfilled later
    {
        EmitOpcode(Opcode.SWITCH);
        EmitU8(numCases);
        for (int i = 0; i < numCases; i++)
        {
            EmitU32(0); // value
            EmitS16(0); // label relative offset
        }
        return Flush();
    }
    private FlushResult EmitCatch() => EmitInst(Opcode.CATCH);
    private FlushResult EmitThrow() => EmitInst(Opcode.THROW);
    #endregion Instruction Emitters

    #region Public High-Level Emitters

    public void EmitScript(ScriptDeclaration script)
    {
        EmitScriptEntryPoint(script);

        while (functionsToCompile.TryDequeue(out var function))
        {
            EmitFunction(function);
        }
    }

    public void EmitScriptEntryPoint(ScriptDeclaration script)
    {
        // TODO: emit static initializers at the beginning of the SCRIPT entrypoint
        EmitFunctionCommon("SCRIPT", script.Parameters, script.Body, VoidType.Instance);
    }

    public void EmitFunction(FunctionDeclaration function)
    {
        Label(function.Name, isFunctionLabel: true);
        EmitFunctionCommon(function.Name, function.Parameters, function.Body, ((FunctionType)function.Semantics.ValueType!).Return);
    }

    private void EmitFunctionCommon(string name, ImmutableArray<VarDeclaration> parameters, ImmutableArray<IStatement> body, TypeInfo returnType)
    {
        currentFunctionReturnType = returnType;
        Debug.Assert(returnType.SizeOf <= byte.MaxValue, $"Return type too big (sizeof: {returnType.SizeOf})");

        currentFunctionFrameSize = 0;
        currentFunctionAllocatedLocals.Clear();
        localLabels.Clear();

        // allocate space for parameters
        var argCount = 0;
        foreach (var p in parameters)
        {
            if (p.Kind is VarKind.ScriptParameter) continue; // ScriptParameters are stored as static variables

            var paramSize = p.Semantics.ValueType!.SizeOf;
            AllocateFrameSpaceForVar(p);
            argCount += paramSize;
        }
        Debug.Assert(argCount <= byte.MaxValue, $"Too many parameters (argCount: {argCount})");
        currentFunctionArgCount = (byte)argCount;

        // allocate space required by the engine to store the return address and caller frame address
        AllocateFrameSpace(2);

        // prologue
        var enter = EmitEnter((byte)argCount, frameSize: 0, name);

        // body
        EmitStatementBlock(body);

        // epilogue
        if (body.LastOrDefault() is not ReturnStatement)
        {
            EmitEpilogue();
        }

        // backfill frame size
        var code = CollectionsMarshal.AsSpan(codeBuffer);
        var frameSizeOperandOffset = codeInstructionOffsets[enter.InstructionIndex] + 2;
        var frameSizeSpan = code[frameSizeOperandOffset..(frameSizeOperandOffset + 2)];
        Debug.Assert(frameSizeSpan[0] == 0 && frameSizeSpan[1] == 0);
        frameSizeSpan[0] = (byte)(currentFunctionFrameSize & 0xFF);
        frameSizeSpan[1] = (byte)(currentFunctionFrameSize >> 8);
    }

    public void EmitEpilogue()
    {
        Debug.Assert(currentFunctionReturnType is not null);
        EmitLeave(currentFunctionArgCount, (byte)currentFunctionReturnType.SizeOf);
    }

    public int AllocateFrameSpace(int size)
    {
        var offset = currentFunctionFrameSize;
        currentFunctionFrameSize += size;
        Debug.Assert(currentFunctionFrameSize <= ushort.MaxValue, $"Function frame size is too big");
        return offset;
    }
    public int AllocateFrameSpaceForVar(VarDeclaration varDecl)
    {
        Debug.Assert(varDecl.Kind is VarKind.Local or VarKind.Parameter);

        if (currentFunctionAllocatedLocals.ContainsKey(varDecl))
        {
            throw new ArgumentException($"Var '{varDecl.Name}' is already allocated", nameof(varDecl));
        }

        var size = varDecl.Semantics.ValueType!.SizeOf;
        var offset = AllocateFrameSpace(size);
        currentFunctionAllocatedLocals.Add(varDecl, offset);
        return offset;
    }

    public void EmitStatementBlock(ImmutableArray<IStatement> statements)
    {
        foreach (var stmt in statements) EmitStatement(stmt);
    }
    public void EmitStatement(IStatement stmt)
    {
        if (stmt.Label is not null)
        {
            Label(stmt.Label.Name);
        }

        stmt.Accept(stmtEmitter);
    }
    public void EmitValue(IExpression expr) => expr.Accept(valueEmitter);
    public void EmitValueAndDrop(IExpression expr)
    {
        EmitValue(expr);
        var valueSize = expr.Semantics.Type!.SizeOf;
        for (int i = 0; i < valueSize; i++)
        {
            EmitDrop();
        }
    }
    public void EmitAddress(IExpression expr) => expr.Accept(addressEmitter);


    public void EmitJump(string label)
    {
        var res = EmitJ(0);
        ReferenceLabel(label, codeInstructionOffsets[res.InstructionIndex] + 1, LabelReferenceKind.Relative, isFunctionLabel: false);
    }
    public void EmitJumpIfZero(string label)
    {
        var res = EmitJZ(0);
        ReferenceLabel(label, codeInstructionOffsets[res.InstructionIndex] + 1, LabelReferenceKind.Relative, isFunctionLabel: false);
    }

    public void EmitCall(FunctionDeclaration function)
    {
        var res = EmitCall(0);
        ReferenceLabel(function.Name, codeInstructionOffsets[res.InstructionIndex] + 1, LabelReferenceKind.Absolute, isFunctionLabel: true);
        OnFunctionFound(function);
    }

    public void EmitFunctionAddress(FunctionDeclaration function)
    {
        var res = EmitPushConstU24(0);
        ReferenceLabel(function.Name, codeInstructionOffsets[res.InstructionIndex] + 1, LabelReferenceKind.Absolute, isFunctionLabel: true);
        OnFunctionFound(function);
    }

    //public void EmitNativeCall(int argsSize, int returnSize, string label) => Emit(Opcode.NATIVE, argsSize, returnSize, label);

    public void EmitSwitch(IEnumerable<ValueSwitchCase> valueCases)
    {
        throw new NotImplementedException(nameof(EmitSwitch));
        var cases = valueCases.ToArray();
        Debug.Assert(cases.Length <= byte.MaxValue, $"Too many SWITCH cases (numCases: {cases.Length})");
        var res = EmitSwitch((byte)cases.Length);
        for (int i = 0; i < cases.Length; i++)
        {
            var @case = cases[i];
            var valueAddress = codeInstructionOffsets[res.InstructionIndex] + 1 + i * 6;
            // TODO: fill value from @case.Value
            var labelRelativeOffsetAddress = valueAddress + 4;
            ReferenceLabel(@case.Semantics.Label!, labelRelativeOffsetAddress, LabelReferenceKind.Relative, isFunctionLabel: false);
        }
    }

    public void EmitLoadFrom(IExpression lvalueExpr)
    {
        Debug.Assert(lvalueExpr.Semantics.ValueKind.Is(ValueKind.Addressable));

        var size = lvalueExpr.Type!.SizeOf;
        if (size == 1)
        {
            EmitAddress(lvalueExpr);
            EmitLoad();
        }
        else
        {
            EmitPushInt(size);
            EmitAddress(lvalueExpr);
            EmitLoadN();
        }
    }

    public void EmitStoreAt(IExpression lvalueExpr)
    {
        Debug.Assert(lvalueExpr.Semantics.ValueKind.Is(ValueKind.Addressable));

        var size = lvalueExpr.Type!.SizeOf;
        if (size == 1)
        {
            EmitAddress(lvalueExpr);
            EmitStore();
        }
        else
        {
            EmitPushInt(size);
            EmitAddress(lvalueExpr);
            EmitStoreN();
        }
    }

    public void EmitStoreAt(VarDeclaration varDecl)
    {
        Debug.Assert(varDecl.Kind is VarKind.Local or VarKind.Static or VarKind.Global);

        var size = varDecl.Semantics.ValueType!.SizeOf;
        if (size == 1)
        {
            EmitVarAddress(varDecl);
            EmitStore();
        }
        else
        {
            EmitPushInt(size);
            EmitVarAddress(varDecl);
            EmitStoreN();
        }
    }

    public void EmitArrayIndexing(IndexingExpression expr)
    {
        EmitValue(expr.Index);
        EmitAddress(expr.Array);

        var itemSize = expr.Semantics.Type!.SizeOf;
        switch (itemSize)
        {
            case >= byte.MinValue and <= byte.MaxValue:
                EmitArrayU8((byte)itemSize);
                break;

            case >= ushort.MinValue and <= ushort.MaxValue:
                EmitArrayU16((ushort)itemSize);
                break;

            default:
                Debug.Assert(false, $"Array item size too big (itemSize: {itemSize})");
                break;
        }
    }

    public void EmitOffset(int offset)
    {
        switch (offset)
        {
            case >= byte.MinValue and <= byte.MaxValue:
                EmitIOffsetU8((byte)offset);
                break;

            case >= short.MinValue and <= short.MaxValue:
                EmitIOffsetS16((short)offset);
                break;

            default:
                EmitPushInt(offset);
                EmitIOffset();
                break;
        }
    }

    private void EmitGlobalAddress(VarDeclaration declaration)
    {
        throw new NotImplementedException(nameof(EmitGlobalAddress));
        // TODO: EmitGlobal
        //switch (varDecl.Address)
        //{
        //    case >= 0 and <= 0x0000FFFF:
        //        CG.Emit(Opcode.GLOBAL_U16, varDecl.Address);
        //        break;

        //    case >= 0 and <= 0x00FFFFFF:
        //        CG.Emit(Opcode.GLOBAL_U24, varDecl.Address);
        //        break;

        //    default: Debug.Assert(false, "Global var address too big"); break;
        //}
    }
    private void EmitStaticAddress(VarDeclaration declaration)
    {
        throw new NotImplementedException(nameof(EmitStaticAddress));
        // TODO: EmitStaticAddress
        //switch (varDecl.Address)
        //{
        //    case >= 0 and <= 0x000000FF:
        //        CG.Emit(Opcode.STATIC_U8, varDecl.Address);
        //        break;

        //    case >= 0 and <= 0x0000FFFF:
        //        CG.Emit(Opcode.STATIC_U16, varDecl.Address);
        //        break;

        //    default: Debug.Assert(false, "Static var address too big"); break;
        //}
    }
    private void EmitScriptParameterAddress(VarDeclaration declaration) => EmitStaticAddress(declaration);
    private void EmitLocalAddress(VarDeclaration declaration)
    {
        var address = currentFunctionAllocatedLocals[declaration];
        switch (address)
        {
            case >= byte.MinValue and <= byte.MaxValue:
                EmitLocalU8((byte)address);
                break;

            case >= ushort.MinValue and <= ushort.MaxValue:
                EmitLocalU16((ushort)address);
                break;

            default: Debug.Assert(false, "Local var address too big"); break;
        }
    }
    private void EmitParameterAddress(VarDeclaration declaration)
    {
        if (declaration.IsReference)
        {
            // parameter passed by reference, the address is its value
            var paramAddress = currentFunctionAllocatedLocals[declaration];
            switch (paramAddress)
            {
                case >= byte.MinValue and <= byte.MaxValue:
                    EmitLocalU8Load((byte)paramAddress);
                    break;

                case >= ushort.MinValue and <= ushort.MaxValue:
                    EmitLocalU16Load((ushort)paramAddress);
                    break;

                default: Debug.Assert(false, "Parameter address too big"); break;
            }
        }
        else
        {
            // parameter passed by value, treat it as a local variable
            EmitLocalAddress(declaration);
        }
    }

    public void EmitVarAddress(VarDeclaration declaration)
    {
        switch (declaration.Kind)
        {
            case VarKind.Global: EmitGlobalAddress(declaration); break;
            case VarKind.Static: EmitStaticAddress(declaration); break;
            case VarKind.ScriptParameter: EmitScriptParameterAddress(declaration); break;
            case VarKind.Local: EmitLocalAddress(declaration); break;
            case VarKind.Parameter: EmitParameterAddress(declaration); break;

            case VarKind.Constant: Debug.Assert(false, "Cannot get address of constant var"); break;
            case VarKind.Field: Debug.Assert(false, "Cannot get address of field directly"); break;
            default: Debug.Assert(false, "Unknown var kind"); break;
        }
    }

    /// <summary>
    /// Emits code to default initialize a local variable.
    /// </summary>
    public void EmitDefaultInit(VarDeclaration declaration)
    {
        Debug.Assert(declaration.Kind is VarKind.Local);
        EmitLocalAddress(declaration);
        EmitDefaultInitNoPushAddress(declaration.Semantics.ValueType!);
        EmitDrop(); // drop local address
    }

    private void EmitDefaultInitNoPushAddress(TypeInfo type)
    {
        switch (type)
        {
            case StructType ty: EmitDefaultInitStruct(ty); break;
            case ArrayType ty: EmitDefaultInitArray(ty); break;
            default: throw new ArgumentException($"Cannot default initialize type '{type.ToPrettyString()}'", nameof(type));
        }
    }

    private void EmitDefaultInitStruct(StructType structTy)
    {
        throw new NotImplementedException(nameof(EmitDefaultInitStruct));
        foreach (var field in structTy.Fields)
        {
            var hasInitializer = false; // TODO: get initializer from struct field field.Initializer is not null;
            if (hasInitializer/* || TypeHelper.IsDefaultInitialized(field.Type)*/)
            {
                EmitDup(); // duplicate struct address
                //if (field.Offset != 0)
                {
                    //EmitOffset(field.Offset); // advance to field offset
                }

                // initialize field
                if (hasInitializer)
                {
                    switch (field.Type)
                    {
                        case IntType:
                            //EmitPushInt(ExpressionEvaluator.EvalInt(field.Initializer!, CG.Symbols));
                            EmitStoreRev();
                            break;
                        case FloatType:
                            // TODO: game scripts use PUSH_CONST_U32 to default initialize FLOAT fields, should we change it?
                            //EmitPushFloat(ExpressionEvaluator.EvalFloat(field.Initializer!, CG.Symbols));
                            EmitStoreRev();
                            break;
                        case BoolType:
                            //EmitPushBool(ExpressionEvaluator.EvalBool(field.Initializer!, CG.Symbols) ? 1 : 0);
                            EmitStoreRev();
                            break;
                        // TODO: should VECTOR or STRING fields be allowed to be default initialized? it doesn't seem to happen in the game scripts
                        //case StructType sTy when BuiltInTypes.IsVectorType(sTy):
                        //    DefaultInitVector(field.Initializer!);
                        //    break;
                        default: throw new System.NotImplementedException();
                    }
                }
                else
                {
                    //Debug.Assert(TypeHelper.IsDefaultInitialized(field.Type));
                    //EmitDefaultInitNoPushAddress(field.Type);
                }

                EmitDrop(); // drop duplicated address
            }
        }
    }

    private void EmitDefaultInitArray(ArrayType arrayTy)
    {
        throw new NotImplementedException(nameof(EmitDefaultInitArray));
        // write array size
        EmitPushInt(arrayTy.Length);
        EmitStoreRev();

        //if (TypeHelper.IsDefaultInitialized(arrayTy.ItemType))
        {
            EmitDup(); // duplicate array address
            EmitOffset(1); // advance duplicated address to the first item (skip array size)
            var itemSize = arrayTy.Item.SizeOf;
            for (int i = 0; i < arrayTy.Length; i++)
            {
                //EmitDefaultInitNoPushAddress(arrayTy.Item); // initialize item
                EmitOffset(itemSize); // advance to the next item
            }
            EmitDrop(); // drop duplicated address
        }
    }

    public void EmitPushString(string value)
    {
        var offset = Strings.GetOffsetOf(value);
        EmitPushInt(offset);
        EmitString();
    }

    public void EmitPushNull() => EmitPushInt(0);
    public void EmitPushBool(bool value) => EmitPushInt(value ? 1 : 0);
    public void EmitPushInt(int value)
    {
        switch (value)
        {
            case -1: EmitPushConstM1(); break;
            case 0: EmitPushConst0(); break;
            case 1: EmitPushConst1(); break;
            case 2: EmitPushConst2(); break;
            case 3: EmitPushConst3(); break;
            case 4: EmitPushConst4(); break;
            case 5: EmitPushConst5(); break;
            case 6: EmitPushConst6(); break;
            case 7: EmitPushConst7(); break;

            case >= byte.MinValue and <= byte.MaxValue:
                EmitPushConstU8((byte)value);
                break;

            case >= short.MinValue and <= short.MaxValue:
                EmitPushConstS16((short)value);
                break;

            case >= 0 and <= 0x00FFFFFF:
                EmitPushConstU24(unchecked((uint)value));
                break;

            default:
                EmitPushConstU32(unchecked((uint)value));
                break;
        }
    }

    public void EmitPushFloat(float value)
    {
        switch (value)
        {
            case -1.0f: EmitPushConstFM1(); break;
            case 0.0f: EmitPushConstF0(); break;
            case 1.0f: EmitPushConstF1(); break;
            case 2.0f: EmitPushConstF2(); break;
            case 3.0f: EmitPushConstF3(); break;
            case 4.0f: EmitPushConstF4(); break;
            case 5.0f: EmitPushConstF5(); break;
            case 6.0f: EmitPushConstF6(); break;
            case 7.0f: EmitPushConstF7(); break;
            default: EmitPushConstF(value); break;
        }
    }

    public void EmitPushConst(ConstantValue value)
    {
        value.Match(
            caseNull: EmitPushNull,
            caseInt: EmitPushInt,
            caseFloat: EmitPushFloat,
            caseBool: EmitPushBool,
            caseString: EmitPushString,
            caseVector: (x, y, z) =>
            {
                EmitPushFloat(x);
                EmitPushFloat(y);
                EmitPushFloat(z);
            });
    }
    #endregion Public High-Level Emitters

    private readonly Dictionary<string, LabelInfo> functionLabels = new(ParserNew.CaseInsensitiveComparer);
    private readonly Dictionary<string, LabelInfo> localLabels = new(ParserNew.CaseInsensitiveComparer);

    public void Label(string name, bool isFunctionLabel = false)
    {
        var labels = isFunctionLabel ? functionLabels : localLabels;

        var labelAddress = codeBuffer.Count;
        if (labels.TryGetValue(name, out var info))
        {
            // the label already appeared before
            if (info.UnresolvedReferences is not null)
            {
                // backfill label references
                foreach (var reference in info.UnresolvedReferences)
                {
                    BackfillLabel(labelAddress, reference);
                }
                labels[name] = new(Address: labelAddress, UnresolvedReferences: null);
            }
            else
            {
                // the label appeared before and the references are already resolved, so this is a re-definition
                Debug.Assert(false, $"Label name '{name}' is repeated");
            }
        }
        else
        {
            // no references to backfill
            labels.Add(name, new(Address: labelAddress, UnresolvedReferences: null));
        }
    }

    // TODO: operandAddress to instruction index + operand offset?
    private void ReferenceLabel(string label, int operandAddress, LabelReferenceKind kind, bool isFunctionLabel)
    {
        var labels = isFunctionLabel ? functionLabels : localLabels;

        if (labels.TryGetValue(label, out var info))
        {
            if (info.IsResolved)
            {
                // the label address is known
                Debug.Assert(info.Address.HasValue);
                BackfillLabel(info.Address.Value, new(operandAddress, kind));
            }
            else
            {
                // the label address is unknown
                Debug.Assert(info.UnresolvedReferences is not null);
                info.UnresolvedReferences.Add(new(operandAddress, kind));
            }
        }
        else
        {
            // first time this label is referenced
            labels[label] = new(Address: null, UnresolvedReferences: new() { new(operandAddress, kind) });
        }
    }

    private void BackfillLabel(int labelAddress, LabelReference reference)
    {
        var code = CollectionsMarshal.AsSpan(codeBuffer);
        switch (reference.Kind)
        {
            case LabelReferenceKind.Absolute:
                var destU24 = code[reference.OperandAddress..(reference.OperandAddress + 3)];
                Debug.Assert(destU24[0] == 0 && destU24[1] == 0 && destU24[2] == 0);
                destU24[0] = (byte)(labelAddress & 0xFF);
                destU24[1] = (byte)((labelAddress >> 8) & 0xFF);
                destU24[2] = (byte)((labelAddress >> 16) & 0xFF);
                return;
            case LabelReferenceKind.Relative:
                var destS16 = code[reference.OperandAddress..(reference.OperandAddress + 2)];
                Debug.Assert(destS16[0] == 0 && destS16[1] == 0);
                var relOffset = AbsoluteAddressToOperandRelativeOffset(labelAddress, reference.OperandAddress);
                destS16[0] = (byte)(relOffset & 0xFF);
                destS16[1] = (byte)(relOffset >> 8);
                return;
        }
    }

    private short AbsoluteAddressToOperandRelativeOffset(int absoluteAddress, int operandAddress)
    {
        var relOffset = absoluteAddress - (operandAddress + 2);
        if (relOffset < short.MinValue || relOffset > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException($"Address is too far", nameof(absoluteAddress));
        }

        return (short)relOffset;
        //dest[0] = (byte)(relOffset & 0xFF);
        //dest[1] = (byte)((relOffset >> 8) & 0xFF);
    }

    private enum LabelReferenceKind { Relative, Absolute }
    private record struct LabelReference(int OperandAddress, LabelReferenceKind Kind);
    private record struct LabelInfo(int? Address, List<LabelReference>? UnresolvedReferences)
    {
        public bool IsResolved => Address != null;
    }

    private void OnFunctionFound(FunctionDeclaration function)
    {
        if (usedFunctions.Add(function))
        {
            functionsToCompile.Enqueue(function);
        }
    }
}
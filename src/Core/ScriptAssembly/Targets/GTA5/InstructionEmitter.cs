namespace ScTools.ScriptAssembly.Targets.GTA5;
internal class InstructionEmitter : InstructionEmitter<OpcodeV10>
{
    private const bool IncludeFunctionNames = true;

    public InstructionEmitter(IInstructionEmitterFlushStrategy flushStrategy) : base(flushStrategy)
    {
    }

    public InstructionReference EmitNop() => EmitInst(OpcodeV10.NOP);
    public InstructionReference EmitIAdd() => EmitInst(OpcodeV10.IADD);
    public InstructionReference EmitIAddU8(byte value) => EmitInstU8(OpcodeV10.IADD_U8, value);
    public InstructionReference EmitIAddS16(short value) => EmitInstS16(OpcodeV10.IADD_S16, value);
    public InstructionReference EmitISub() => EmitInst(OpcodeV10.ISUB);
    public InstructionReference EmitIMul() => EmitInst(OpcodeV10.IMUL);
    public InstructionReference EmitIMulU8(byte value) => EmitInstU8(OpcodeV10.IMUL_U8, value);
    public InstructionReference EmitIMulS16(short value) => EmitInstS16(OpcodeV10.IMUL_S16, value);
    public InstructionReference EmitIDiv() => EmitInst(OpcodeV10.IDIV);
    public InstructionReference EmitIMod() => EmitInst(OpcodeV10.IMOD);
    public InstructionReference EmitINot() => EmitInst(OpcodeV10.INOT);
    public InstructionReference EmitINeg() => EmitInst(OpcodeV10.INEG);
    public InstructionReference EmitIEq() => EmitInst(OpcodeV10.IEQ);
    public InstructionReference EmitINe() => EmitInst(OpcodeV10.INE);
    public InstructionReference EmitIGt() => EmitInst(OpcodeV10.IGT);
    public InstructionReference EmitIGe() => EmitInst(OpcodeV10.IGE);
    public InstructionReference EmitILt() => EmitInst(OpcodeV10.ILT);
    public InstructionReference EmitILe() => EmitInst(OpcodeV10.ILE);
    public InstructionReference EmitFAdd() => EmitInst(OpcodeV10.FADD);
    public InstructionReference EmitFSub() => EmitInst(OpcodeV10.FSUB);
    public InstructionReference EmitFMul() => EmitInst(OpcodeV10.FMUL);
    public InstructionReference EmitFDiv() => EmitInst(OpcodeV10.FDIV);
    public InstructionReference EmitFMod() => EmitInst(OpcodeV10.FMOD);
    public InstructionReference EmitFNeg() => EmitInst(OpcodeV10.FNEG);
    public InstructionReference EmitFEq() => EmitInst(OpcodeV10.FEQ);
    public InstructionReference EmitFNe() => EmitInst(OpcodeV10.FNE);
    public InstructionReference EmitFGt() => EmitInst(OpcodeV10.FGT);
    public InstructionReference EmitFGe() => EmitInst(OpcodeV10.FGE);
    public InstructionReference EmitFLt() => EmitInst(OpcodeV10.FLT);
    public InstructionReference EmitFLe() => EmitInst(OpcodeV10.FLE);
    public InstructionReference EmitVAdd() => EmitInst(OpcodeV10.VADD);
    public InstructionReference EmitVSub() => EmitInst(OpcodeV10.VSUB);
    public InstructionReference EmitVMul() => EmitInst(OpcodeV10.VMUL);
    public InstructionReference EmitVDiv() => EmitInst(OpcodeV10.VDIV);
    public InstructionReference EmitVNeg() => EmitInst(OpcodeV10.VNEG);
    public InstructionReference EmitIAnd() => EmitInst(OpcodeV10.IAND);
    public InstructionReference EmitIOr() => EmitInst(OpcodeV10.IOR);
    public InstructionReference EmitIXor() => EmitInst(OpcodeV10.IXOR);
    public InstructionReference EmitI2F() => EmitInst(OpcodeV10.I2F);
    public InstructionReference EmitF2I() => EmitInst(OpcodeV10.F2I);
    public InstructionReference EmitF2V() => EmitInst(OpcodeV10.F2V);
    public InstructionReference EmitDup() => EmitInst(OpcodeV10.DUP);
    public InstructionReference EmitDrop() => EmitInst(OpcodeV10.DROP);
    public InstructionReference EmitLoad() => EmitInst(OpcodeV10.LOAD);
    public InstructionReference EmitLoadN() => EmitInst(OpcodeV10.LOAD_N);
    public InstructionReference EmitStore() => EmitInst(OpcodeV10.STORE);
    public InstructionReference EmitStoreN() => EmitInst(OpcodeV10.STORE_N);
    public InstructionReference EmitStoreRev() => EmitInst(OpcodeV10.STORE_REV);
    public InstructionReference EmitString() => EmitInst(OpcodeV10.STRING);
    public InstructionReference EmitStringHash() => EmitInst(OpcodeV10.STRINGHASH);
    public InstructionReference EmitPushConstM1() => EmitInst(OpcodeV10.PUSH_CONST_M1);
    public InstructionReference EmitPushConst0() => EmitInst(OpcodeV10.PUSH_CONST_0);
    public InstructionReference EmitPushConst1() => EmitInst(OpcodeV10.PUSH_CONST_1);
    public InstructionReference EmitPushConst2() => EmitInst(OpcodeV10.PUSH_CONST_2);
    public InstructionReference EmitPushConst3() => EmitInst(OpcodeV10.PUSH_CONST_3);
    public InstructionReference EmitPushConst4() => EmitInst(OpcodeV10.PUSH_CONST_4);
    public InstructionReference EmitPushConst5() => EmitInst(OpcodeV10.PUSH_CONST_5);
    public InstructionReference EmitPushConst6() => EmitInst(OpcodeV10.PUSH_CONST_6);
    public InstructionReference EmitPushConst7() => EmitInst(OpcodeV10.PUSH_CONST_7);
    public InstructionReference EmitPushConstFM1() => EmitInst(OpcodeV10.PUSH_CONST_FM1);
    public InstructionReference EmitPushConstF0() => EmitInst(OpcodeV10.PUSH_CONST_F0);
    public InstructionReference EmitPushConstF1() => EmitInst(OpcodeV10.PUSH_CONST_F1);
    public InstructionReference EmitPushConstF2() => EmitInst(OpcodeV10.PUSH_CONST_F2);
    public InstructionReference EmitPushConstF3() => EmitInst(OpcodeV10.PUSH_CONST_F3);
    public InstructionReference EmitPushConstF4() => EmitInst(OpcodeV10.PUSH_CONST_F4);
    public InstructionReference EmitPushConstF5() => EmitInst(OpcodeV10.PUSH_CONST_F5);
    public InstructionReference EmitPushConstF6() => EmitInst(OpcodeV10.PUSH_CONST_F6);
    public InstructionReference EmitPushConstF7() => EmitInst(OpcodeV10.PUSH_CONST_F7);
    public InstructionReference EmitPushConstU8(byte value) => EmitInstU8(OpcodeV10.PUSH_CONST_U8, value);
    public InstructionReference EmitPushConstU8U8(byte value1, byte value2) => EmitInstU8U8(OpcodeV10.PUSH_CONST_U8_U8, value1, value2);
    public InstructionReference EmitPushConstU8U8U8(byte value1, byte value2, byte value3) => EmitInstU8U8U8(OpcodeV10.PUSH_CONST_U8_U8_U8, value1, value2, value3);
    public InstructionReference EmitPushConstS16(short value) => EmitInstS16(OpcodeV10.PUSH_CONST_S16, value);
    public InstructionReference EmitPushConstU24(uint value) => EmitInstU24(OpcodeV10.PUSH_CONST_U24, value);
    public InstructionReference EmitPushConstU32(uint value) => EmitInstU32(OpcodeV10.PUSH_CONST_U32, value);
    public InstructionReference EmitPushConstF(float value) => EmitInstF32(OpcodeV10.PUSH_CONST_F, value);
    public InstructionReference EmitArrayU8(byte itemSize) => EmitInstU8(OpcodeV10.ARRAY_U8, itemSize);
    public InstructionReference EmitArrayU8Load(byte itemSize) => EmitInstU8(OpcodeV10.ARRAY_U8_LOAD, itemSize);
    public InstructionReference EmitArrayU8Store(byte itemSize) => EmitInstU8(OpcodeV10.ARRAY_U8_STORE, itemSize);
    public InstructionReference EmitArrayU16(ushort itemSize) => EmitInstU16(OpcodeV10.ARRAY_U16, itemSize);
    public InstructionReference EmitArrayU16Load(ushort itemSize) => EmitInstU16(OpcodeV10.ARRAY_U16_LOAD, itemSize);
    public InstructionReference EmitArrayU16Store(ushort itemSize) => EmitInstU16(OpcodeV10.ARRAY_U16_STORE, itemSize);
    public InstructionReference EmitLocalU8(byte frameOffset) => EmitInstU8(OpcodeV10.LOCAL_U8, frameOffset);
    public InstructionReference EmitLocalU8Load(byte frameOffset) => EmitInstU8(OpcodeV10.LOCAL_U8_LOAD, frameOffset);
    public InstructionReference EmitLocalU8Store(byte frameOffset) => EmitInstU8(OpcodeV10.LOCAL_U8_STORE, frameOffset);
    public InstructionReference EmitLocalU16(ushort frameOffset) => EmitInstU16(OpcodeV10.LOCAL_U16, frameOffset);
    public InstructionReference EmitLocalU16Load(ushort frameOffset) => EmitInstU16(OpcodeV10.LOCAL_U16_LOAD, frameOffset);
    public InstructionReference EmitLocalU16Store(ushort frameOffset) => EmitInstU16(OpcodeV10.LOCAL_U16_STORE, frameOffset);
    public InstructionReference EmitStaticU8(byte staticOffset) => EmitInstU8(OpcodeV10.STATIC_U8, staticOffset);
    public InstructionReference EmitStaticU8Load(byte staticOffset) => EmitInstU8(OpcodeV10.STATIC_U8_LOAD, staticOffset);
    public InstructionReference EmitStaticU8Store(byte staticOffset) => EmitInstU8(OpcodeV10.STATIC_U8_STORE, staticOffset);
    public InstructionReference EmitStaticU16(ushort staticOffset) => EmitInstU16(OpcodeV10.STATIC_U16, staticOffset);
    public InstructionReference EmitStaticU16Load(ushort staticOffset) => EmitInstU16(OpcodeV10.STATIC_U16_LOAD, staticOffset);
    public InstructionReference EmitStaticU16Store(ushort staticOffset) => EmitInstU16(OpcodeV10.STATIC_U16_STORE, staticOffset);
    public InstructionReference EmitGlobalU16(ushort globalOffset) => EmitInstU16(OpcodeV10.GLOBAL_U16, globalOffset);
    public InstructionReference EmitGlobalU16Load(ushort globalOffset) => EmitInstU16(OpcodeV10.GLOBAL_U16_LOAD, globalOffset);
    public InstructionReference EmitGlobalU16Store(ushort globalOffset) => EmitInstU16(OpcodeV10.GLOBAL_U16_STORE, globalOffset);
    public InstructionReference EmitGlobalU24(uint globalOffset) => EmitInstU24(OpcodeV10.GLOBAL_U24, globalOffset);
    public InstructionReference EmitGlobalU24Load(uint globalOffset) => EmitInstU24(OpcodeV10.GLOBAL_U24_LOAD, globalOffset);
    public InstructionReference EmitGlobalU24Store(uint globalOffset) => EmitInstU24(OpcodeV10.GLOBAL_U24_STORE, globalOffset);
    public InstructionReference EmitIOffset() => EmitInst(OpcodeV10.IOFFSET);
    public InstructionReference EmitIOffsetU8(byte offset) => EmitInstU8(OpcodeV10.IOFFSET_U8, offset);
    public InstructionReference EmitIOffsetU8Load(byte offset) => EmitInstU8(OpcodeV10.IOFFSET_U8_LOAD, offset);
    public InstructionReference EmitIOffsetU8Store(byte offset) => EmitInstU8(OpcodeV10.IOFFSET_U8_STORE, offset);
    public InstructionReference EmitIOffsetS16(short offset) => EmitInstS16(OpcodeV10.IOFFSET_S16, offset);
    public InstructionReference EmitIOffsetS16Load(short offset) => EmitInstS16(OpcodeV10.IOFFSET_S16_LOAD, offset);
    public InstructionReference EmitIOffsetS16Store(short offset) => EmitInstS16(OpcodeV10.IOFFSET_S16_STORE, offset);
    public InstructionReference EmitTextLabelAssignString(byte textLabelLength) => EmitInstU8(OpcodeV10.TEXT_LABEL_ASSIGN_STRING, textLabelLength);
    public InstructionReference EmitTextLabelAssignInt(byte textLabelLength) => EmitInstU8(OpcodeV10.TEXT_LABEL_ASSIGN_INT, textLabelLength);
    public InstructionReference EmitTextLabelAppendString(byte textLabelLength) => EmitInstU8(OpcodeV10.TEXT_LABEL_APPEND_STRING, textLabelLength);
    public InstructionReference EmitTextLabelAppendInt(byte textLabelLength) => EmitInstU8(OpcodeV10.TEXT_LABEL_APPEND_INT, textLabelLength);
    public InstructionReference EmitTextLabelCopy() => EmitInst(OpcodeV10.TEXT_LABEL_COPY);
    public InstructionReference EmitNative(byte argCount, byte returnCount, ushort nativeIndex)
    {
        Debug.Assert((argCount & 0x3F) == argCount); // arg count max bits 6
        Debug.Assert((returnCount & 0x3) == returnCount); // arg count max bits 2
        return EmitInstU8U8U8(OpcodeV10.NATIVE,
            operand1: (byte)((argCount & 0x3F) << 2 | (returnCount & 0x3)),
            operand2: (byte)((nativeIndex >> 8) & 0xFF),
            operand3: (byte)(nativeIndex & 0xFF));
    }
    public InstructionReference EmitEnter(byte argCount, ushort frameSize, string? name)
    {
        EmitOpcode(OpcodeV10.ENTER);
        EmitU8(argCount);
        EmitU16(frameSize);
        if (IncludeFunctionNames && name is not null)
        {
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(name).AsSpan();
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
    public InstructionReference EmitLeave(byte argCount, byte returnCount) => EmitInstU8U8(OpcodeV10.LEAVE, argCount, returnCount);
    public InstructionReference EmitJ(short relativeOffset) => EmitInstS16(OpcodeV10.J, relativeOffset);
    public InstructionReference EmitJZ(short relativeOffset) => EmitInstS16(OpcodeV10.JZ, relativeOffset);
    public InstructionReference EmitIEqJZ(short relativeOffset) => EmitInstS16(OpcodeV10.IEQ_JZ, relativeOffset);
    public InstructionReference EmitINeJZ(short relativeOffset) => EmitInstS16(OpcodeV10.INE_JZ, relativeOffset);
    public InstructionReference EmitIGtJZ(short relativeOffset) => EmitInstS16(OpcodeV10.IGT_JZ, relativeOffset);
    public InstructionReference EmitIGeJZ(short relativeOffset) => EmitInstS16(OpcodeV10.IGE_JZ, relativeOffset);
    public InstructionReference EmitILtJZ(short relativeOffset) => EmitInstS16(OpcodeV10.ILT_JZ, relativeOffset);
    public InstructionReference EmitILeJZ(short relativeOffset) => EmitInstS16(OpcodeV10.ILE_JZ, relativeOffset);
    public InstructionReference EmitCall(uint functionOffset) => EmitInstU24(OpcodeV10.CALL, functionOffset);
    public InstructionReference EmitCallIndirect() => EmitInst(OpcodeV10.CALLINDIRECT);
    public InstructionReference EmitSwitch((uint Value, short RelativeOffset)[] cases) // cases will be backfilled later
    {
        Debug.Assert(cases.Length <= byte.MaxValue, $"Too many SWITCH cases (numCases: {cases.Length})");
        EmitOpcode(OpcodeV10.SWITCH);
        EmitU8((byte)cases.Length);
        for (int i = 0; i < cases.Length; i++)
        {
            EmitU32(cases[i].Value); // value
            EmitS16(cases[i].RelativeOffset); // label relative offset
        }
        return Flush();
    }
    public InstructionReference EmitCatch() => EmitInst(OpcodeV10.CATCH);
    public InstructionReference EmitThrow() => EmitInst(OpcodeV10.THROW);
}

namespace ScTools.ScriptAssembly.Targets.NY;

using ScTools.ScriptAssembly;

internal class InstructionEmitter : InstructionEmitter<OpcodeNY>
{
    public InstructionEmitter(IInstructionEmitterFlushStrategy flushStrategy) : base(flushStrategy)
    {
    }

    private void EmitLengthPrefixedString(string s)
    {
        Debug.Assert(s.Length <= byte.MaxValue, $"String is too long to fit length in a single byte");

        EmitU8((byte)s.Length);
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        foreach (var b in bytes)
        {
            EmitU8(b);
        }
    }

    private InstructionReference EmitInstStr(OpcodeNY opcode, string operand)
    {
        EmitOpcode(opcode);
        EmitLengthPrefixedString(operand);
        return Flush();
    }

    public InstructionReference EmitIAdd() => EmitInst(OpcodeNY.IADD);
    public InstructionReference EmitISub() => EmitInst(OpcodeNY.ISUB);
    public InstructionReference EmitIMul() => EmitInst(OpcodeNY.IMUL);
    public InstructionReference EmitIDiv() => EmitInst(OpcodeNY.IDIV);
    public InstructionReference EmitIMod() => EmitInst(OpcodeNY.IMOD);
    public InstructionReference EmitINot() => EmitInst(OpcodeNY.INOT);
    public InstructionReference EmitINeg() => EmitInst(OpcodeNY.INEG);
    public InstructionReference EmitIEq() => EmitInst(OpcodeNY.IEQ);
    public InstructionReference EmitINe() => EmitInst(OpcodeNY.INE);
    public InstructionReference EmitIGt() => EmitInst(OpcodeNY.IGT);
    public InstructionReference EmitIGe() => EmitInst(OpcodeNY.IGE);
    public InstructionReference EmitILt() => EmitInst(OpcodeNY.ILT);
    public InstructionReference EmitILe() => EmitInst(OpcodeNY.ILE);
    public InstructionReference EmitFAdd() => EmitInst(OpcodeNY.FADD);
    public InstructionReference EmitFSub() => EmitInst(OpcodeNY.FSUB);
    public InstructionReference EmitFMul() => EmitInst(OpcodeNY.FMUL);
    public InstructionReference EmitFDiv() => EmitInst(OpcodeNY.FDIV);
    public InstructionReference EmitFMod() => EmitInst(OpcodeNY.FMOD);
    public InstructionReference EmitFNeg() => EmitInst(OpcodeNY.FNEG);
    public InstructionReference EmitFEq() => EmitInst(OpcodeNY.FEQ);
    public InstructionReference EmitFNe() => EmitInst(OpcodeNY.FNE);
    public InstructionReference EmitFGt() => EmitInst(OpcodeNY.FGT);
    public InstructionReference EmitFGe() => EmitInst(OpcodeNY.FGE);
    public InstructionReference EmitFLt() => EmitInst(OpcodeNY.FLT);
    public InstructionReference EmitFLe() => EmitInst(OpcodeNY.FLE);
    public InstructionReference EmitVAdd() => EmitInst(OpcodeNY.VADD);
    public InstructionReference EmitVSub() => EmitInst(OpcodeNY.VSUB);
    public InstructionReference EmitVMul() => EmitInst(OpcodeNY.VMUL);
    public InstructionReference EmitVDiv() => EmitInst(OpcodeNY.VDIV);
    public InstructionReference EmitVNeg() => EmitInst(OpcodeNY.VNEG);
    public InstructionReference EmitIAnd() => EmitInst(OpcodeNY.IAND);
    public InstructionReference EmitIOr() => EmitInst(OpcodeNY.IOR);
    public InstructionReference EmitIXor() => EmitInst(OpcodeNY.IXOR);
    public InstructionReference EmitJ(uint offset) => EmitInstU32(OpcodeNY.J, offset);
    public InstructionReference EmitJZ(uint offset) => EmitInstU32(OpcodeNY.JZ, offset);
    public InstructionReference EmitJNZ(uint offset) => EmitInstU32(OpcodeNY.JNZ, offset);
    public InstructionReference EmitI2F() => EmitInst(OpcodeNY.I2F);
    public InstructionReference EmitF2I() => EmitInst(OpcodeNY.F2I);
    public InstructionReference EmitF2V() => EmitInst(OpcodeNY.F2V);
    public InstructionReference EmitPushConstU16(ushort value) => EmitInstU16(OpcodeNY.PUSH_CONST_U16, value);
    public InstructionReference EmitPushConstU32(uint value) => EmitInstU32(OpcodeNY.PUSH_CONST_U32, value);
    public InstructionReference EmitPushConstF(float value) => EmitInstF32(OpcodeNY.PUSH_CONST_F, value);
    public InstructionReference EmitDup() => EmitInst(OpcodeNY.DUP);
    public InstructionReference EmitDrop() => EmitInst(OpcodeNY.DROP);
    public InstructionReference EmitNative(byte paramCount, byte returnCount, uint commandHash) => EmitInstU8U8U32(OpcodeNY.NATIVE, paramCount, returnCount, commandHash);
    public InstructionReference EmitCall(uint functionOffset) => EmitInstU32(OpcodeNY.CALL, functionOffset);
    public InstructionReference EmitEnter(byte paramCount, ushort frameSize) => EmitInstU8U16(OpcodeNY.ENTER, paramCount, frameSize);
    public InstructionReference EmitLeave(byte paramCount, byte returnCount) => EmitInstU8U8(OpcodeNY.LEAVE, paramCount, returnCount);
    public InstructionReference EmitLoad() => EmitInst(OpcodeNY.LOAD);
    public InstructionReference EmitStore() => EmitInst(OpcodeNY.STORE);
    public InstructionReference EmitStoreRev() => EmitInst(OpcodeNY.STORE_REV);
    public InstructionReference EmitLoadN() => EmitInst(OpcodeNY.LOAD_N);
    public InstructionReference EmitStoreN() => EmitInst(OpcodeNY.STORE_N);
    public InstructionReference EmitLocalN(int localIndex)
    {
        Debug.Assert(localIndex is >= 0 and <= 7, "Only 0 to 7 is supported");
        return EmitInst((OpcodeNY)((int)OpcodeNY.LOCAL_0 + localIndex));
    }
    public InstructionReference EmitLocal() => EmitInst(OpcodeNY.LOCAL);
    public InstructionReference EmitStatic() => EmitInst(OpcodeNY.STATIC);
    public InstructionReference EmitGlobal() => EmitInst(OpcodeNY.GLOBAL);
    public InstructionReference EmitArray() => EmitInst(OpcodeNY.ARRAY);
    public InstructionReference EmitSwitch(uint[] cases) // cases jump offset will be backfilled later
    {
        Debug.Assert(cases.Length <= byte.MaxValue, $"Too many SWITCH cases (numCases: {cases.Length})");
        EmitOpcode(OpcodeNY.SWITCH);
        EmitU8((byte)cases.Length);
        for (int i = 0; i < cases.Length; i++)
        {
            EmitU32(cases[i]); // value
            EmitU32(0); // label offset
        }
        return Flush();
    }
    public InstructionReference EmitString(string str) => EmitInstStr(OpcodeNY.STRING, str);
    public InstructionReference EmitNull() => EmitInst(OpcodeNY.NULL);
    public InstructionReference EmitTextLabelAssignString(byte textLabelLength) => EmitInstU8(OpcodeNY.TEXT_LABEL_ASSIGN_STRING, textLabelLength);
    public InstructionReference EmitTextLabelAssignInt(byte textLabelLength) => EmitInstU8(OpcodeNY.TEXT_LABEL_ASSIGN_INT, textLabelLength);
    public InstructionReference EmitTextLabelAppendString(byte textLabelLength) => EmitInstU8(OpcodeNY.TEXT_LABEL_APPEND_STRING, textLabelLength);
    public InstructionReference EmitTextLabelAppendInt(byte textLabelLength) => EmitInstU8(OpcodeNY.TEXT_LABEL_APPEND_INT, textLabelLength);
    public InstructionReference EmitCatch() => EmitInst(OpcodeNY.CATCH);
    public InstructionReference EmitThrow() => EmitInst(OpcodeNY.THROW);
    public InstructionReference EmitTextLabelCopy() => EmitInst(OpcodeNY.TEXT_LABEL_COPY);
    public InstructionReference EmitXProtectLoad() => EmitInst(OpcodeNY._XPROTECT_LOAD);
    public InstructionReference EmitXProtectStore() => EmitInst(OpcodeNY._XPROTECT_STORE);
    public InstructionReference EmitXProtectRef() => EmitInst(OpcodeNY._XPROTECT_REF);
    public InstructionReference EmitPushConstN(int n)
    {
        Debug.Assert(n is >= -16 and <= 159, "Only -16 to 159 is supported");
        return EmitInst((OpcodeNY)((int)OpcodeNY.PUSH_CONST_0 + n));
    }
}

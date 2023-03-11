namespace ScTools.ScriptAssembly.Targets.NY;
internal class InstructionEmitter : InstructionEmitter<Opcode>
{
    public InstructionEmitter(IInstructionEmitterFlushStrategy flushStrategy) : base(flushStrategy)
    {
    }

    private void EmitLengthPrefixedString(string s)
    {
        Debug.Assert(s.Length < byte.MaxValue, $"String is too long to fit length in a single byte");
        Debug.Assert(s.Length < byte.MaxValue, $"String is too long to fit length in a single byte");

        EmitU8((byte)(s.Length + 1));
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        foreach (var b in bytes)
        {
            EmitU8(b);
        }
        EmitU8(0);
    }

    private InstructionReference EmitInstStr(Opcode opcode, string operand)
    {
        EmitOpcode(opcode);
        EmitLengthPrefixedString(operand);
        return Flush();
    }

    public InstructionReference EmitIAdd() => EmitInst(Opcode.IADD);
    public InstructionReference EmitISub() => EmitInst(Opcode.ISUB);
    public InstructionReference EmitIMul() => EmitInst(Opcode.IMUL);
    public InstructionReference EmitIDiv() => EmitInst(Opcode.IDIV);
    public InstructionReference EmitIMod() => EmitInst(Opcode.IMOD);
    public InstructionReference EmitINot() => EmitInst(Opcode.INOT);
    public InstructionReference EmitINeg() => EmitInst(Opcode.INEG);
    public InstructionReference EmitIEq() => EmitInst(Opcode.IEQ);
    public InstructionReference EmitINe() => EmitInst(Opcode.INE);
    public InstructionReference EmitIGt() => EmitInst(Opcode.IGT);
    public InstructionReference EmitIGe() => EmitInst(Opcode.IGE);
    public InstructionReference EmitILt() => EmitInst(Opcode.ILT);
    public InstructionReference EmitILe() => EmitInst(Opcode.ILE);
    public InstructionReference EmitFAdd() => EmitInst(Opcode.FADD);
    public InstructionReference EmitFSub() => EmitInst(Opcode.FSUB);
    public InstructionReference EmitFMul() => EmitInst(Opcode.FMUL);
    public InstructionReference EmitFDiv() => EmitInst(Opcode.FDIV);
    public InstructionReference EmitFMod() => EmitInst(Opcode.FMOD);
    public InstructionReference EmitFNeg() => EmitInst(Opcode.FNEG);
    public InstructionReference EmitFEq() => EmitInst(Opcode.FEQ);
    public InstructionReference EmitFNe() => EmitInst(Opcode.FNE);
    public InstructionReference EmitFGt() => EmitInst(Opcode.FGT);
    public InstructionReference EmitFGe() => EmitInst(Opcode.FGE);
    public InstructionReference EmitFLt() => EmitInst(Opcode.FLT);
    public InstructionReference EmitFLe() => EmitInst(Opcode.FLE);
    public InstructionReference EmitVAdd() => EmitInst(Opcode.VADD);
    public InstructionReference EmitVSub() => EmitInst(Opcode.VSUB);
    public InstructionReference EmitVMul() => EmitInst(Opcode.VMUL);
    public InstructionReference EmitVDiv() => EmitInst(Opcode.VDIV);
    public InstructionReference EmitVNeg() => EmitInst(Opcode.VNEG);
    public InstructionReference EmitIAnd() => EmitInst(Opcode.IAND);
    public InstructionReference EmitIOr() => EmitInst(Opcode.IOR);
    public InstructionReference EmitIXor() => EmitInst(Opcode.IXOR);
    public InstructionReference EmitJ(uint offset) => EmitInstU32(Opcode.J, offset);
    public InstructionReference EmitJZ(uint offset) => EmitInstU32(Opcode.JZ, offset);
    public InstructionReference EmitJNZ(uint offset) => EmitInstU32(Opcode.JNZ, offset);
    public InstructionReference EmitI2F() => EmitInst(Opcode.I2F);
    public InstructionReference EmitF2I() => EmitInst(Opcode.F2I);
    public InstructionReference EmitF2V() => EmitInst(Opcode.F2V);
    public InstructionReference EmitPushConstU16(ushort value) => EmitInstU16(Opcode.PUSH_CONST_U16, value);
    public InstructionReference EmitPushConstU32(uint value) => EmitInstU32(Opcode.PUSH_CONST_U32, value);
    public InstructionReference EmitPushConstF(float value) => EmitInstF32(Opcode.PUSH_CONST_F, value);
    public InstructionReference EmitDup() => EmitInst(Opcode.DUP);
    public InstructionReference EmitDrop() => EmitInst(Opcode.DROP);
    public InstructionReference EmitNative(byte paramCount, byte returnCount, uint commandHash) => EmitInstU8U8U32(Opcode.NATIVE, paramCount, returnCount, commandHash);
    public InstructionReference EmitCall(uint functionOffset) => EmitInstU32(Opcode.CALL, functionOffset);
    public InstructionReference EmitEnter(byte paramCount, ushort frameSize) => EmitInstU8U16(Opcode.ENTER, paramCount, frameSize);
    public InstructionReference EmitLeave(byte paramCount, byte returnCount) => EmitInstU8U8(Opcode.LEAVE, paramCount, returnCount);
    public InstructionReference EmitLoad() => EmitInst(Opcode.LOAD);
    public InstructionReference EmitStore() => EmitInst(Opcode.STORE);
    public InstructionReference EmitStoreRev() => EmitInst(Opcode.STORE_REV);
    public InstructionReference EmitLoadN() => EmitInst(Opcode.LOAD_N);
    public InstructionReference EmitStoreN() => EmitInst(Opcode.STORE_N);
    public InstructionReference EmitLocalN(int localIndex)
    {
        Debug.Assert(localIndex is >= 0 and <= 7, "Only 0 to 7 is supported");
        return EmitInst((Opcode)((int)Opcode.LOCAL_0 + localIndex));
    }
    public InstructionReference EmitLocal() => EmitInst(Opcode.LOCAL);
    public InstructionReference EmitStatic() => EmitInst(Opcode.STATIC);
    public InstructionReference EmitGlobal() => EmitInst(Opcode.GLOBAL);
    public InstructionReference EmitArray() => EmitInst(Opcode.ARRAY);
    public InstructionReference EmitSwitch((uint Value, uint Offset)[] cases)
    {
        Debug.Assert(cases.Length <= byte.MaxValue, $"Too many SWITCH cases (numCases: {cases.Length})");
        EmitOpcode(Opcode.SWITCH);
        EmitU8((byte)cases.Length);
        for (int i = 0; i < cases.Length; i++)
        {
            EmitU32(cases[i].Value); // value
            EmitU32(cases[i].Offset); // label offset
        }
        return Flush();
    }
    public InstructionReference EmitString(string str) => EmitInstStr(Opcode.STRING, str);
    public InstructionReference EmitNull() => EmitInst(Opcode.NULL);
    public InstructionReference EmitTextLabelAssignString(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_ASSIGN_STRING, textLabelLength);
    public InstructionReference EmitTextLabelAssignInt(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_ASSIGN_INT, textLabelLength);
    public InstructionReference EmitTextLabelAppendString(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_APPEND_STRING, textLabelLength);
    public InstructionReference EmitTextLabelAppendInt(byte textLabelLength) => EmitInstU8(Opcode.TEXT_LABEL_APPEND_INT, textLabelLength);
    public InstructionReference EmitCatch() => EmitInst(Opcode.CATCH);
    public InstructionReference EmitThrow() => EmitInst(Opcode.THROW);
    public InstructionReference EmitTextLabelCopy() => EmitInst(Opcode.TEXT_LABEL_COPY);
    public InstructionReference EmitXProtectLoad() => EmitInst(Opcode._XPROTECT_LOAD);
    public InstructionReference EmitXProtectStore() => EmitInst(Opcode._XPROTECT_STORE);
    public InstructionReference EmitXProtectRef() => EmitInst(Opcode._XPROTECT_REF);
    public InstructionReference EmitPushConstN(int n)
    {
        Debug.Assert(n is >= -16 and <= 159, "Only -16 to 159 is supported");
        return EmitInst((Opcode)((int)Opcode.PUSH_CONST_0 + n));
    }
}

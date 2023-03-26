namespace ScTools.ScriptAssembly.Targets.MP3;
internal class InstructionEmitter : InstructionEmitter<Opcode>
{
    private const bool IncludeFunctionNames = true;

    public InstructionEmitter(IInstructionEmitterFlushStrategy flushStrategy) : base(flushStrategy)
    {
    }

    private void EmitLengthPrefixedString(string s)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        var lengthWithNull = bytes.Length + 1;
        Debug.Assert(lengthWithNull <= ushort.MaxValue, $"String is too long to fit in the instruction");

        if (lengthWithNull > byte.MaxValue)
        {
            EmitU8(0);
            EmitU16((ushort)lengthWithNull);
        }
        else
        {
            EmitU8((byte)lengthWithNull);
        }
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

    public InstructionReference EmitNop() => EmitInst(Opcode.NOP);
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
    public InstructionReference EmitEnter(byte paramCount, ushort frameSize, string? name)
    {
        EmitOpcode(Opcode.ENTER);
        EmitU8(paramCount);
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
    public InstructionReference EmitCallIndirect() => EmitInst(Opcode.CALLINDIRECT);
    public InstructionReference EmitPushConstN(int n)
    {
        Debug.Assert(n is >= -16 and <= 159, "Only -16 to 159 is supported");
        return EmitInst((Opcode)((int)Opcode.PUSH_CONST_0 + n));
    }
}

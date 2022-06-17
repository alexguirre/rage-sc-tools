namespace ScTools.ScriptAssembly.Targets;

using ScTools.ScriptAssembly;

internal abstract class InstructionEmitter<TOpcode>
    where TOpcode : struct, Enum
{
    private readonly List<byte> instructionBuffer = new();
    public IInstructionEmitterFlushStrategy FlushStrategy { get; set; }

    public InstructionEmitter(IInstructionEmitterFlushStrategy flushStrategy)
        => FlushStrategy = flushStrategy;

    /// <summary>
    /// Clears the current instruction buffer.
    /// </summary>
    protected void Drop()
    {
        instructionBuffer.Clear();
    }


    /// <summary>
    /// Writes the current instruction buffer.
    /// </summary>
    protected InstructionReference Flush()
    {
        var instRef = FlushStrategy.Flush(instructionBuffer);
        Drop();
        return instRef;
    }

    #region Byte Emitters
    protected void EmitBytes(ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes)
        {
            instructionBuffer.Add(b);
        }
    }

    protected void EmitU8(byte v)
    {
        instructionBuffer.Add(v);
    }

    protected void EmitU16(ushort v)
    {
        instructionBuffer.Add((byte)(v & 0xFF));
        instructionBuffer.Add((byte)(v >> 8));
    }

    protected void EmitS16(short v) => EmitU16(unchecked((ushort)v));

    protected void EmitU24(uint v)
    {
        Debug.Assert((v & 0xFFFFFF) == v);
        instructionBuffer.Add((byte)(v & 0xFF));
        instructionBuffer.Add((byte)((v >> 8) & 0xFF));
        instructionBuffer.Add((byte)((v >> 16) & 0xFF));
    }

    protected void EmitU32(uint v)
    {
        instructionBuffer.Add((byte)(v & 0xFF));
        instructionBuffer.Add((byte)((v >> 8) & 0xFF));
        instructionBuffer.Add((byte)((v >> 16) & 0xFF));
        instructionBuffer.Add((byte)(v >> 24));
    }

    protected unsafe void EmitF32(float v) => EmitU32(*(uint*)&v);

    protected void EmitOpcode(TOpcode v) => EmitU8(v.AsInteger<TOpcode, byte>());
    #endregion Byte Emitters

    #region Instruction Emitters
    /// <summary>
    /// Emits an instruction of length 0. Used as label marker.
    /// </summary>
    public InstructionReference EmitLabelMarker()
        => Flush();
    protected InstructionReference EmitInst(TOpcode opcode)
    {
        EmitOpcode(opcode);
        return Flush();
    }
    protected InstructionReference EmitInstU8(TOpcode opcode, byte operand)
    {
        EmitOpcode(opcode);
        EmitU8(operand);
        return Flush();
    }
    protected InstructionReference EmitInstU8U8(TOpcode opcode, byte operand1, byte operand2)
    {
        EmitOpcode(opcode);
        EmitU8(operand1);
        EmitU8(operand2);
        return Flush();
    }
    protected InstructionReference EmitInstU8U16(TOpcode opcode, byte operand1, ushort operand2)
    {
        EmitOpcode(opcode);
        EmitU8(operand1);
        EmitU16(operand2);
        return Flush();
    }
    protected InstructionReference EmitInstU8U8U8(TOpcode opcode, byte operand1, byte operand2, byte operand3)
    {
        EmitOpcode(opcode);
        EmitU8(operand1);
        EmitU8(operand2);
        EmitU8(operand3);
        return Flush();
    }
    protected InstructionReference EmitInstU8U8U32(TOpcode opcode, byte operand1, byte operand2, uint operand3)
    {
        EmitOpcode(opcode);
        EmitU8(operand1);
        EmitU8(operand2);
        EmitU32(operand3);
        return Flush();
    }
    protected InstructionReference EmitInstS16(TOpcode opcode, short operand)
    {
        EmitOpcode(opcode);
        EmitS16(operand);
        return Flush();
    }
    protected InstructionReference EmitInstU16(TOpcode opcode, ushort operand)
    {
        EmitOpcode(opcode);
        EmitU16(operand);
        return Flush();
    }
    protected InstructionReference EmitInstU24(TOpcode opcode, uint operand)
    {
        EmitOpcode(opcode);
        EmitU24(operand);
        return Flush();
    }
    protected InstructionReference EmitInstU32(TOpcode opcode, uint operand)
    {
        EmitOpcode(opcode);
        EmitU32(operand);
        return Flush();
    }
    protected InstructionReference EmitInstF32(TOpcode opcode, float operand)
    {
        EmitOpcode(opcode);
        EmitF32(operand);
        return Flush();
    }
    #endregion Instruction Emitters
}

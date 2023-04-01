namespace ScTools.ScriptAssembly.Targets.GTA4;

public abstract class OpcodeTraits : IOpcodeTraits<Opcode>
{
    public const int NumberOfOpcodes = 256;

    static int IOpcodeTraits<Opcode>.NumberOfOpcodes => NumberOfOpcodes;

    /// <returns>
    /// The byte size of a instruction with this <paramref name="opcode"/>; or, <c>0</c> if the size is variable (i.e. <paramref name="opcode"/> is <see cref="Opcode.SWITCH"/> or <see cref="Opcode.STRING"/>).
    /// </returns>
    public static int ConstantByteSize(Opcode opcode)
        => opcode switch
        {
            Opcode.TEXT_LABEL_ASSIGN_STRING or
            Opcode.TEXT_LABEL_ASSIGN_INT or
            Opcode.TEXT_LABEL_APPEND_STRING or
            Opcode.TEXT_LABEL_APPEND_INT => 2,

            Opcode.PUSH_CONST_U16 or
            Opcode.LEAVE => 3,

            Opcode.ENTER => 4,

            Opcode.J or
            Opcode.JZ or
            Opcode.JNZ or
            Opcode.PUSH_CONST_U32 or
            Opcode.PUSH_CONST_F or
            Opcode.CALL => 5,

            Opcode.NATIVE => 7,

            Opcode.SWITCH or
            Opcode.STRING => 0,

            _ => 1,
        };

    public static int ByteSize(ReadOnlySpan<byte> bytecode)
    {
        var opcode = (Opcode)bytecode[0];
        var s = opcode switch
        {
            Opcode.SWITCH => 8 * bytecode[1] + 2,
            Opcode.STRING => bytecode[1] + 2,
            _ => ConstantByteSize(opcode),
        };

        return s;
    }

    public static Span<byte> GetInstructionSpan(Span<byte> code, int address)
    {
        var inst = code[address..];
        var instLength = ByteSize(inst);
        return inst[..instLength]; // trim to instruction length
    }

    public static ReadOnlySpan<byte> GetInstructionSpan(ReadOnlySpan<byte> code, int address)
    {
        var inst = code[address..];
        var instLength = ByteSize(inst);
        return inst[..instLength]; // trim to instruction length
    }
}

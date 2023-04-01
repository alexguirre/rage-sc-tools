namespace ScTools.ScriptAssembly.Targets.MP3;

public abstract class OpcodeTraits : IOpcodeTraits<Opcode>
{
    public const int NumberOfOpcodes = 256;

    static int IOpcodeTraits<Opcode>.NumberOfOpcodes => NumberOfOpcodes;

    public static int ConstantByteSize(Opcode opcode)
        => opcode switch
        {
            Opcode.TEXT_LABEL_ASSIGN_STRING or
            Opcode.TEXT_LABEL_ASSIGN_INT or
            Opcode.TEXT_LABEL_APPEND_STRING or
            Opcode.TEXT_LABEL_APPEND_INT => 2,

            Opcode.PUSH_CONST_U16 or
            Opcode.LEAVE => 3,

            Opcode.J or
            Opcode.JZ or
            Opcode.JNZ or
            Opcode.PUSH_CONST_U32 or
            Opcode.PUSH_CONST_F or
            Opcode.CALL => 5,

            Opcode.NATIVE => 7,

            Opcode.ENTER or
            Opcode.SWITCH or
            Opcode.STRING => 0,

            _ => 1,
        };

    public static int ByteSize(ReadOnlySpan<byte> bytecode)
    {
        var opcode = (Opcode)bytecode[0];
        var s = opcode switch
        {
            Opcode.ENTER => bytecode[4] + 5,
            Opcode.SWITCH => 8 * bytecode[1] + 2,
            Opcode.STRING => SizeOfSTRING(bytecode),
            _ => ConstantByteSize(opcode),
        };

        return s;

        static int SizeOfSTRING(ReadOnlySpan<byte> bytecode)
        {
            int size = 2;
            int strLength = bytecode[1];
            if (strLength == 0)
            {
                // long string
                strLength = bytecode[2] | (bytecode[3] << 8);
                size += 2;
            }
            size += strLength;
            return size;
        }
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

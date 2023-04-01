namespace ScTools.ScriptAssembly.Targets.RDR2;

using System.Buffers.Binary;

public abstract class OpcodeTraits : IOpcodeTraits<Opcode>
{
    public const int NumberOfOpcodes = 256;

    static int IOpcodeTraits<Opcode>.NumberOfOpcodes => NumberOfOpcodes;

    public static int ConstantByteSize(Opcode opcode)
        => opcode switch
        {
            Opcode.PUSH_CONST_U8 or
            Opcode.ARRAY_U8 or
            Opcode.ARRAY_U8_LOAD or
            Opcode.ARRAY_U8_STORE or
            Opcode.LOCAL_U8 or
            Opcode.LOCAL_U8_LOAD or
            Opcode.LOCAL_U8_STORE or
            Opcode.STATIC_U8 or
            Opcode.STATIC_U8_LOAD or
            Opcode.STATIC_U8_STORE or
            Opcode.IADD_U8 or
            Opcode.IOFFSET_U8_LOAD or
            Opcode.IOFFSET_U8_STORE or
            Opcode.IMUL_U8 or
            Opcode.TEXT_LABEL_ASSIGN_STRING or
            Opcode.TEXT_LABEL_ASSIGN_INT or
            Opcode.TEXT_LABEL_APPEND_STRING or
            Opcode.TEXT_LABEL_APPEND_INT => 2,

            Opcode.PUSH_CONST_U8_U8 or
            Opcode.NATIVE or
            Opcode.LEAVE or
            Opcode.PUSH_CONST_S16 or
            Opcode.IADD_S16 or
            Opcode.IOFFSET_S16_LOAD or
            Opcode.IOFFSET_S16_STORE or
            Opcode.IMUL_S16 or
            Opcode.ARRAY_U16 or
            Opcode.ARRAY_U16_LOAD or
            Opcode.ARRAY_U16_STORE or
            Opcode.LOCAL_U16 or
            Opcode.LOCAL_U16_LOAD or
            Opcode.LOCAL_U16_STORE or
            Opcode.STATIC_U16 or
            Opcode.STATIC_U16_LOAD or
            Opcode.STATIC_U16_STORE or
            Opcode.GLOBAL_U16 or
            Opcode.GLOBAL_U16_LOAD or
            Opcode.GLOBAL_U16_STORE or
            (>= Opcode.CALL_0 and <= Opcode.CALL_F) or
            (>= Opcode.J and <= Opcode.ILE_JZ) => 3,

            Opcode.PUSH_CONST_U8_U8_U8 or
            Opcode.GLOBAL_U24 or
            Opcode.GLOBAL_U24_LOAD or
            Opcode.GLOBAL_U24_STORE or
            Opcode.PUSH_CONST_U24 => 4,

            Opcode.PUSH_CONST_U32 or
            Opcode.PUSH_CONST_F => 5,

            Opcode.ENTER or
            Opcode.SWITCH or
            Opcode.STRING or
            Opcode.STRING_U32 => 0,

            _ => 1,
        };

    public static int ByteSize(ReadOnlySpan<byte> bytecode)
    {
        var opcode = (Opcode)bytecode[0];
        var s = opcode switch
        {
            Opcode.ENTER => bytecode[4] + 5,
            Opcode.SWITCH => 6 * bytecode[1] + 2,
            Opcode.STRING => bytecode[1] + 2,
            Opcode.STRING_U32 => (int)(BinaryPrimitives.ReadUInt32LittleEndian(bytecode[1..]) + 5),
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

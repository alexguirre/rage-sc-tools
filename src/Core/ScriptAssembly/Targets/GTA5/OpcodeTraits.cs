namespace ScTools.ScriptAssembly.Targets.GTA5;

public interface IOpcodeTraitsGTA5<TOpcode> : IOpcodeTraits<TOpcode> where TOpcode : struct, Enum
{
    public static abstract TOpcode ENTER { get; }
    public static abstract TOpcode SWITCH { get; }
    public static abstract string[] DumpOpcodeFormats { get; }
}

public abstract class OpcodeTraitsV10 : IOpcodeTraitsGTA5<OpcodeV10>
{
    public const int NumberOfOpcodes = 127;

    static int IOpcodeTraits<OpcodeV10>.NumberOfOpcodes => NumberOfOpcodes;
    public static OpcodeV10 ENTER => OpcodeV10.ENTER;

    public static OpcodeV10 SWITCH => OpcodeV10.SWITCH;

    public static int ConstantByteSize(OpcodeV10 opcode)
        => (int)opcode < NumberOfOpcodes ? ByteSizeTable[(int)opcode] : 1;

    public static int ByteSize(ReadOnlySpan<byte> bytecode)
    {
        var opcode = (OpcodeV10)bytecode[0];
        var s = opcode switch
        {
            OpcodeV10.ENTER => bytecode[4] + 5,
            OpcodeV10.SWITCH => 6 * bytecode[1] + 2,
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

    private static readonly byte[] ByteSizeTable = new byte[NumberOfOpcodes]
    {
        1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
        1,1,1,1,1,2,3,4,5,5,1,1,4,0,3,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,1,
        2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,4,4,4,
        4,4,0,1,1,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
    };

    public static string[] DumpOpcodeFormats { get; } = new string[NumberOfOpcodes]
    {
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "b",
        "bb",
        "bbb",
        "d",
        "f",
        "",
        "",
        "bbb",
        "bs$",
        "bb",
        "",
        "",
        "",
        "",
        "",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "",
        "b",
        "b",
        "b",
        "s",
        "s",
        "s",
        "s",
        "s",
        "s",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "R",
        "R",
        "R",
        "R",
        "R",
        "R",
        "R",
        "R",
        "a",
        "a",
        "a",
        "a",
        "a",
        "S",
        "",
        "",
        "b",
        "b",
        "b",
        "b",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
    };
}

public abstract class OpcodeTraitsV11 : IOpcodeTraitsGTA5<OpcodeV11>
{
    public const int NumberOfOpcodes = 128;

    static int IOpcodeTraits<OpcodeV11>.NumberOfOpcodes => NumberOfOpcodes;
    public static OpcodeV11 ENTER => OpcodeV11.ENTER;

    public static OpcodeV11 SWITCH => OpcodeV11.SWITCH;

    public static int ConstantByteSize(OpcodeV11 opcode)
        => (int)opcode < NumberOfOpcodes ? ByteSizeTable[(int)opcode] : 1;

    public static int ByteSize(ReadOnlySpan<byte> bytecode)
    {
        var opcode = (OpcodeV11)bytecode[0];
        var s = opcode switch
        {
            OpcodeV11.ENTER => bytecode[4] + 5,
            OpcodeV11.SWITCH => 6 * bytecode[1] + 2,
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

    private static readonly byte[] ByteSizeTable = new byte[NumberOfOpcodes]
    {
        1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
        1,1,1,1,1,2,3,4,5,5,1,1,4,0,3,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,1,
        2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,4,4,4,
        4,4,0,1,1,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
    };

    public static string[] DumpOpcodeFormats { get; } = new string[NumberOfOpcodes]
    {
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "b",
        "bb",
        "bbb",
        "d",
        "f",
        "",
        "",
        "bbb",
        "bs$",
        "bb",
        "",
        "",
        "",
        "",
        "",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "",
        "b",
        "b",
        "b",
        "s",
        "s",
        "s",
        "s",
        "s",
        "s",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "R",
        "R",
        "R",
        "R",
        "R",
        "R",
        "R",
        "R",
        "a",
        "a",
        "a",
        "a",
        "a",
        "S",
        "",
        "",
        "b",
        "b",
        "b",
        "b",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
    };
}

public abstract class OpcodeTraitsV12 : IOpcodeTraitsGTA5<OpcodeV12>
{
    public const int NumberOfOpcodes = 131;

    static int IOpcodeTraits<OpcodeV12>.NumberOfOpcodes => NumberOfOpcodes;
    public static OpcodeV12 ENTER => OpcodeV12.ENTER;
    public static OpcodeV12 SWITCH => OpcodeV12.SWITCH;

    public static int ConstantByteSize(OpcodeV12 opcode)
        => (int)opcode < NumberOfOpcodes ? ByteSizeTable[(int)opcode] : 1;

    public static int ByteSize(ReadOnlySpan<byte> bytecode)
    {
        var opcode = (OpcodeV12)bytecode[0];
        var s = opcode switch
        {
            OpcodeV12.ENTER => bytecode[4] + 5,
            OpcodeV12.SWITCH => 6 * bytecode[1] + 2,
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

    private static readonly byte[] ByteSizeTable = new byte[NumberOfOpcodes]
    {
        1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
        1,1,1,1,1,2,3,4,5,5,1,1,4,0,3,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,1,
        2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,4,4,4,
        4,4,4,4,4,0,1,1,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
    };

    public static string[] DumpOpcodeFormats { get; } = new string[NumberOfOpcodes]
    {
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "b",
        "bb",
        "bbb",
        "d",
        "f",
        "",
        "",
        "bbb",
        "bs$",
        "bb",
        "",
        "",
        "",
        "",
        "",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "b",
        "",
        "b",
        "b",
        "b",
        "s",
        "s",
        "s",
        "s",
        "s",
        "s",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "h",
        "R",
        "R",
        "R",
        "R",
        "R",
        "R",
        "R",
        "R",
        "a",
        "a",
        "a",
        "a",
        "a",
        "a",
        "a",
        "a",
        "S",
        "",
        "",
        "b",
        "b",
        "b",
        "b",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
        "",
    };
}

namespace ScTools.ScriptAssembly.Targets;

using System;

public readonly ref struct Instruction<TOpcode> where TOpcode : struct, Enum
{
    public bool IsValid => Bytes.Length > 0;
    public int Address { get; init; }
    public ReadOnlySpan<byte> Bytes { get; init; }
    public TOpcode Opcode => Bytes[0].AsEnum<byte, TOpcode>();
}

public ref struct InstructionEnumerator<TOpcode, TTraits>
    where TOpcode : struct, Enum
    where TTraits : IOpcodeTraits<TOpcode>
{
    private readonly ReadOnlySpan<byte> code;
    private int address;

    public Instruction<TOpcode> Current { get; private set; }

    public InstructionEnumerator(ReadOnlySpan<byte> code)
    {
        this.code = code;
        address = 0;
        Current = default;
    }

    public InstructionEnumerator<TOpcode, TTraits> GetEnumerator() => this;

    public bool MoveNext()
    {
        int newAddress = address + Current.Bytes.Length;
        if (newAddress < code.Length)
        {
            address = newAddress;
            Current = new() { Address = address, Bytes = TTraits.GetInstructionSpan(code, address) };
            return true;
        }

        return false;
    }

    public void Reset()
    {
        address = 0;
        Current = default;
    }
}

public static class InstructionEnumeratorScriptExtensions
{
    public static InstructionEnumerator<GTA4.Opcode, GTA4.OpcodeTraits> EnumerateInstructions(this GameFiles.GTA4.Script script)
        => new(script.Code);
    public static InstructionEnumerator<MP3.Opcode, MP3.OpcodeTraits> EnumerateInstructions(this GameFiles.MP3.Script script)
        => new(script.Code);
    public static InstructionEnumerator<RDR2.Opcode, RDR2.OpcodeTraits> EnumerateInstructions(this GameFiles.RDR2.Script script)
        => new(script.MergeCodePages());
    public static InstructionEnumerator<GTA5.OpcodeV12, GTA5.OpcodeTraitsV12> EnumerateInstructions(this GameFiles.GTA5.Script script)
        => new(script.MergeCodePages());
}

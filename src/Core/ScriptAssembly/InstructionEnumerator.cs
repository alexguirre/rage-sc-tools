namespace ScTools.ScriptAssembly;

using ScTools.GameFiles;
using GameFiles.GTA5;
using Targets.GTA5;
using Targets.GTA4;
using System;

public readonly ref struct Instruction<TOpcode> where TOpcode : struct, Enum
{
    public bool IsValid => Bytes.Length > 0;
    public int Address { get; init; }
    public ReadOnlySpan<byte> Bytes { get; init; }
    public TOpcode Opcode => Bytes[0].AsEnum<byte, TOpcode>();
}

public delegate ReadOnlySpan<byte> InstructionSpanGetter(ReadOnlySpan<byte> code, int address);

public ref struct InstructionEnumerator<TOpcode> where TOpcode : struct, Enum
{
    private readonly InstructionSpanGetter instructionSpanGetter; // TODO: move to static interface when updating to C# 11.0
    private readonly ReadOnlySpan<byte> code;
    private int address;

    public Instruction<TOpcode> Current { get; private set; }

    public InstructionEnumerator(ReadOnlySpan<byte> code, InstructionSpanGetter instructionSpanGetter)
    {
        this.code = code;
        this.instructionSpanGetter = instructionSpanGetter;
        address = 0;
        Current = default;
    }

    public InstructionEnumerator<TOpcode> GetEnumerator() => this;

    public bool MoveNext()
    {
        int newAddress = address + Current.Bytes.Length;
        if (newAddress < code.Length)
        {
            address = newAddress;
            Current = new() { Address = address, Bytes = instructionSpanGetter(code, address) };
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
    public static InstructionEnumerator<Opcode> EnumerateInstructions(this ScTools.GameFiles.GTA4.Script script)
        => new(script.Code, Targets.GTA4.OpcodeExtensions.GetInstructionSpan);
    public static InstructionEnumerator<OpcodePayne> EnumerateInstructions(this ScriptPayne script)
        => new(script.Code, OpcodePayneExtensions.GetInstructionSpan);
    public static InstructionEnumerator<OpcodeRDR2> EnumerateInstructions(this ScriptRDR2 script)
        => new(script.MergeCodePages(), OpcodeRDR2Extensions.GetInstructionSpan);
    public static InstructionEnumerator<OpcodeV10> EnumerateInstructions(this ScTools.GameFiles.GTA5.Script script)
        => new(script.MergeCodePages(), Targets.GTA5.OpcodeExtensions.GetInstructionSpan);
}

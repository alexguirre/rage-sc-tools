namespace ScTools.ScriptAssembly.Targets;

internal sealed class InstructionReference
{
    public int Index { get; set; }
}

internal interface IInstructionBuffer
{
    int NumberOfInstructions { get; }
    IReadOnlyList<InstructionReference> Instructions { get; }
    InstructionReference GetRef(int instructionIndex);
    int GetLength(InstructionReference instruction);
    bool IsEmpty(InstructionReference instruction);
    byte GetByte(InstructionReference instruction, int offset);
    List<byte> GetBytes(InstructionReference instruction);
    InstructionReference InsertBefore(InstructionReference instruction, List<byte> instructionBytes);
    InstructionReference InsertAfter(InstructionReference instruction, List<byte> instructionBytes);
    InstructionReference Append(List<byte> instructionBytes);
    void Update(InstructionReference instruction, List<byte> newInstructionBytes);
    void Remove(InstructionReference instruction);
}

internal abstract class InstructionBuffer<TOpcode, TLabelInfo, TResult> : IInstructionBuffer
    where TOpcode : struct, Enum
{
    protected record struct InstructionInfo(int Offset, int Length);

    private const int InitialBufferCapacity = 0x4000;

    private readonly List<byte> buffer = new(capacity: InitialBufferCapacity);
    private readonly List<InstructionInfo> instructions = new(capacity: InitialBufferCapacity / 4);
    private readonly List<InstructionReference> references = new(capacity: InitialBufferCapacity / 4);

    protected List<byte> Buffer => buffer;
    protected List<InstructionInfo> InstructionsInfo => instructions;
    public IReadOnlyList<InstructionReference> Instructions => references;

    public int NumberOfInstructions => instructions.Count;
    public InstructionReference GetRef(int instructionIndex) => references[instructionIndex];
    public int GetLength(InstructionReference instruction) => instructions[instruction.Index].Length;
    public bool IsEmpty(InstructionReference instruction) => instructions[instruction.Index].Length == 0;
    public TOpcode GetOpcode(InstructionReference instruction) => buffer[instructions[instruction.Index].Offset].AsEnum<byte, TOpcode>();
    public byte GetByte(InstructionReference instruction, int offset) => buffer[instructions[instruction.Index].Offset + offset];
    public List<byte> GetBytes(InstructionReference instruction)
    {
        var (instOffset, instLength) = instructions[instruction.Index];
        return buffer.GetRange(instOffset, instLength);
    }

    private InstructionReference InsertInstruction(int index, List<byte> instructionBytes)
    {
        var instOffset = buffer.Count;
        var instLength = instructionBytes.Count;
        var instRef = new InstructionReference { Index = index };
        instructions.Insert(index, new(instOffset, instLength));
        references.Insert(index, instRef);
        buffer.AddRange(instructionBytes);
        UpdateReferences(start: instRef.Index + 1);
        return instRef;
    }

    public InstructionReference InsertBefore(InstructionReference instruction, List<byte> instructionBytes)
        => InsertInstruction(instruction.Index, instructionBytes);

    public InstructionReference InsertAfter(InstructionReference instruction, List<byte> instructionBytes)
        => InsertInstruction(instruction.Index + 1, instructionBytes);

    public InstructionReference Append(List<byte> instructionBytes)
        => InsertInstruction(instructions.Count, instructionBytes);

    public void Update(InstructionReference instruction, List<byte> newInstructionBytes)
    {
        var (instOffset, instLength) = instructions[instruction.Index];
        var newInstLength = newInstructionBytes.Count;
        if (newInstLength <= instLength)
        {
            // can be updated in-place
            for (int i = 0; i < newInstructionBytes.Count; i++)
            {
                buffer[instOffset + i] = newInstructionBytes[i];
            }
            instructions[instruction.Index] = new(instOffset, newInstLength);
        }
        else
        {
            // append bytes to the end
            var newInstOffset = buffer.Count;
            instructions[instruction.Index] = new(newInstOffset, newInstLength);
            buffer.AddRange(newInstructionBytes);
        }
    }

    public void Remove(InstructionReference instruction)
    {
        instructions[instruction.Index] = instructions[instruction.Index] with { Length = 0 };
    }

    private void UpdateReferences(int start = 0)
    {
        for (int i = start; i < references.Count; i++)
        {
            references[i].Index = i;
        }
    }

    public abstract TResult Finish(IEnumerable<TLabelInfo> labels);
}

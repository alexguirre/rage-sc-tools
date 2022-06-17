namespace ScTools.ScriptAssembly.Targets;

internal interface IInstructionEmitterFlushStrategy
{
    InstructionReference Flush(List<byte> instructionBytes);
}

internal sealed class AppendInstructionFlushStrategy : IInstructionEmitterFlushStrategy
{
    public IInstructionBuffer Buffer { get; set; }

    public AppendInstructionFlushStrategy(IInstructionBuffer buffer)
        => Buffer = buffer;

    public InstructionReference Flush(List<byte> instructionBytes)
        => Buffer.Append(instructionBytes);
}

internal sealed class UpdateInstructionFlushStrategy : IInstructionEmitterFlushStrategy
{
    public IInstructionBuffer Buffer { get; set; }
    public InstructionReference InstructionToUpdate { get; set; }

    public UpdateInstructionFlushStrategy(IInstructionBuffer buffer, InstructionReference instructionToUpdate)
    {
        Buffer = buffer;
        InstructionToUpdate = instructionToUpdate;
    }

    public InstructionReference Flush(List<byte> instructionBytes)
    {
        Buffer.Update(InstructionToUpdate, instructionBytes);
        return InstructionToUpdate;
    }
}

internal sealed class InsertAfterInstructionFlushStrategy : IInstructionEmitterFlushStrategy
{
    public IInstructionBuffer Buffer { get; set; }
    public InstructionReference Instruction { get; set; }

    public InsertAfterInstructionFlushStrategy(IInstructionBuffer buffer, InstructionReference instruction)
    {
        Buffer = buffer;
        Instruction = instruction;
    }

    public InstructionReference Flush(List<byte> instructionBytes)
        => Instruction = Buffer.InsertAfter(Instruction, instructionBytes);
}

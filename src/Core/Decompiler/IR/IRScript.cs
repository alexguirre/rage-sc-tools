namespace ScTools.Decompiler.IR;

public class IRScript
{
    public IRInstruction? Head { get; protected set; }
    public IRInstruction? Tail { get; protected set; }

    public void AppendInstruction(IRInstruction instruction)
    {
        if (Head is null)
        {
            Head = instruction;
            Tail = instruction;
        }
        else
        {
            var tail = Tail!;
            tail.Next = instruction;
            instruction.Previous = tail;
            Tail = instruction;
        }
    }
}

namespace ScTools.Decompiler;

using ScTools.Decompiler.IR;

using System.Collections.Immutable;

public class CFGBlock
{
    /// <summary>
    /// Start of the block.
    /// </summary>
    public IRInstruction Start { get; set; }
    /// <summary>
    /// End of the block, exclusive.
    /// </summary>
    public IRInstruction End { get; set; }
    public ImmutableArray<CFGEdge> OutgoingEdges { get; set; }

    public CFGBlock(IRInstruction start, IRInstruction end)
    {
        Start = start;
        End = end;
    }
}

public  enum CFGEdgeKind
{
    /// <summary>
    /// Edge traversed always, unconditionally.
    /// </summary>
    Unconditional,

    /// <summary>
    /// Edge traversed when the condition of a <see cref="Intermediate.Opcode.JZ"/> instruction is zero (false).
    /// </summary>
    IfFalse,
    /// <summary>
    /// Edge traversed when the condition of a <see cref="Intermediate.Opcode.JZ"/> instruction is not zero (true).
    /// </summary>
    IfTrue,

    /// <summary>
    /// Edge is a case of a <see cref="Intermediate.Opcode.SWITCH"/> instruction.
    /// </summary>
    SwitchCase,
}

public record CFGEdge(CFGBlock Source, CFGBlock Target, CFGEdgeKind Kind, int? SwitchCaseValue = null)
{
}

public class CFG
{
    
}

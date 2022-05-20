namespace ScTools.Decompiler;

using ScTools.Decompiler.IR;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

public class CFGBlock : IEnumerable<IRInstruction>
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

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<IRInstruction> IEnumerable<IRInstruction>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<IRInstruction>
    {
        private readonly CFGBlock block;
        private IRInstruction? current;
        private IRInstruction? iter;

        public IRInstruction Current => current!;
        object IEnumerator.Current => Current;

        public Enumerator(CFGBlock block)
        {
            this.block = block;
            current = null;
            iter = block.Start;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (iter is null || iter == block.End)
            {
                current = null;
                return false;
            }

            current = iter;
            iter = iter.Next;
            return true;
        }

        public void Reset()
        {
            current = null;
            iter = block.Start;
        }
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

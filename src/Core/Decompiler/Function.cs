namespace ScTools.Decompiler;

using ScTools.Decompiler.IR;

using System;
using System.Collections.Generic;

public class Function
{
    public IRScript Script { get; }
    public CFGBlock RootBlock { get; }
    public IRInstruction Start => RootBlock.Start;
    public int StartAddress => Start.Address;
    public UnresolvedTypeTuple Parameters { get; }
    public UnresolvedType ReturnType { get; }
    public UnresolvedTypeTuple Locals { get; }

    public Function(IRScript script, IRInstruction start)
    {
        Script = script;
        RootBlock = CFGBuilder.BuildFrom(script, start);
        var enter = FindEnterInstruction();
        var leave = FindLeaveInstruction();
        ReturnType = new(leave.ReturnCount);
        Parameters = new(enter.ParamCount);
        Locals = new(enter.LocalCount);
    }

    public CFGBlock GetBlockStartingAtAddress(int address)
    {
        // TODO: this can be optimized by using a dictionary
        var blocks = new Queue<CFGBlock>();
        var exploredBlocks = new HashSet<CFGBlock>();
        blocks.Enqueue(RootBlock);
        while (blocks.TryDequeue(out var block))
        {
            if (block.Start.Address == address)
            {
                return block;
            }

            exploredBlocks.Add(block);
            foreach (var edge in block.OutgoingEdges)
            {
                if (!exploredBlocks.Contains(edge.Target))
                {
                    blocks.Enqueue(edge.Target);
                }
            }
        }

        throw new ArgumentException($"No block starts at {address:000000}", nameof(address));
    }

    private IREnter FindEnterInstruction() => (IREnter?)FindInstruction(inst => inst is IREnter) ?? throw new ArgumentException("Function is missing a ENTER instruciton");
    private IRLeave FindLeaveInstruction() => (IRLeave?)FindInstruction(inst => inst is IRLeave) ?? throw new ArgumentException("Function is missing a LEAVE instruciton");
    private IRInstruction? FindInstruction(Predicate<IRInstruction> predicate)
    {
        var blocks = new Queue<CFGBlock>();
        var exploredBlocks = new HashSet<CFGBlock>();
        blocks.Enqueue(RootBlock);
        while (blocks.TryDequeue(out var block))
        {
            foreach (var inst in block)
            {
                if (predicate(inst))
                {
                    return inst;
                }
            }

            exploredBlocks.Add(block);
            foreach (var edge in block.OutgoingEdges)
            {
                if (!exploredBlocks.Contains(edge.Target))
                {
                    blocks.Enqueue(edge.Target);
                }
            }
        }

        return null;
    }
}

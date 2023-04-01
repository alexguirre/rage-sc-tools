namespace ScTools.Decompiler;

using ScTools.Decompiler.IR;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public sealed class CFGBuilder
{
    public static CFGBlock BuildFrom(IRCode code, IRInstruction startInstruction)
        => new CFGBuilder(code, startInstruction).BuildGraph();

    private class CFGBlockInProgress
    {
        public int StartAddress { get; set; }
        public int EndAddress { get; set; }
        public IRInstruction? FirstInstruction { get; set; }
        public IRInstruction? LastInstruction { get; set; }
        public CFGEdgeInProgress[] Successors { get; set; } = Array.Empty<CFGEdgeInProgress>();

        public bool IsEmpty => StartAddress == EndAddress;
    }

    private readonly struct CFGEdgeInProgress
    {
        /// <summary>
        /// Gets the block this edge originates from.
        /// </summary>
        public CFGBlockInProgress From { get; init; }
        /// <summary>
        /// Gets the block this edge ends in.
        /// </summary>
        public CFGBlockInProgress To { get; init; }
        public CFGEdgeKind Kind { get; init; }
        /// <summary>
        /// If <see cref="Kind"/> is <see cref="ControlFlowEdgeKind.SwitchCase"/>, gets the value needed to go through this edge.
        /// A <c>null</c> value represents the default case.
        /// </summary>
        public int? SwitchCaseValue { get; init; }
    }

    private readonly IRCode code;
    private readonly IRInstruction start;
    private readonly Dictionary<int, CFGBlockInProgress> blocksByStartAddress = new();

    private CFGBuilder(IRCode code, IRInstruction startInstruction)
    {
        this.code = code;
        start = startInstruction;
    }

    private CFGBlock BuildGraph()
    {
        StartBlocks();
        EndBlocks();
        ConnectBlocks();
        RemoveUnreachableBlocks();

        // convert internal representation to public representation
        var blocks = blocksByStartAddress.ToDictionary(kvp => kvp.Value, kvp => ConvertBlock(kvp.Value));
        foreach (var (inProgressBlock, block) in blocks)
        {
            var outgoing = ImmutableArray.CreateBuilder<CFGEdge>(inProgressBlock.Successors.Length);
            foreach (var successor in inProgressBlock.Successors)
            {
                outgoing.Add(new(blocks[successor.From], blocks[successor.To], successor.Kind, successor.SwitchCaseValue));
            }
            block.OutgoingEdges = outgoing.MoveToImmutable();
        }

        return blocks[blocksByStartAddress[start.Address]];

        CFGBlock ConvertBlock(CFGBlockInProgress block)
            => new(block.FirstInstruction!, block.LastInstruction!);
    }

    private void StartBlocks()
    {
        blocksByStartAddress.Clear();

        StartBlock(start.Address);
        for (var inst = start; inst is not null; inst = inst.Next)
        {
            switch (inst)
            {
                case IRJump j:
                    // start a new block at the target
                    StartBlock(j.JumpAddress);
                    break;
                case IRJumpIfZero jz:
                    // start a new block at the target and at the next instruction
                    StartBlock(jz.JumpAddress);
                    if (jz.Next is not null)
                    {
                        StartBlock(jz.Next.Address);
                    }
                    break;
                case IRSwitch sw:
                    foreach (var c in sw.Cases)
                    {
                        StartBlock(c.JumpAddress);
                    }

                    // start a new block after the switch instruction (normally a jump to the default case)
                    if (sw.Next is not null)
                    {
                        StartBlock(sw.Next.Address);
                    }
                    break;
                case IRLeave l:
                    //
                    break;
            }
        }
    }

    private void EndBlocks()
    {
        if (blocksByStartAddress.Count == 0)
        {
            return;
        }

        foreach (var (startAddress, block) in blocksByStartAddress)
        {
            var inst = block.FirstInstruction;
            var lastInst = inst;
            while (inst is not null)
            {
                if (inst is IRJump or IRJumpIfZero or IRSwitch or IRLeave)
                {
                    lastInst = inst;
                    break;
                }

                if (inst is IREndOfScript || blocksByStartAddress.ContainsKey(inst.Address))
                {
                    lastInst = inst.Previous;
                    break;
                }

                inst = inst.Next;
            }

            if (lastInst is not null)
            {
                block.EndAddress = lastInst!.Next!.Address;
                block.LastInstruction = lastInst;
            }
        }

        // TODO: D:\sources\gtav-sc-tools\src\Cli\bin\x64\Debug\net7.0\sc-tools.exe dump --ir -o ".\mydecompiled_scripts\" -t mp3-x86 ".\_scripts\busdepot_gar_paint.sco"
        // TODO: this logic may be flawed, it assumes that the blocks are contiguous, which doesn't seem to always be the case. Investigate MP3 scripts
       /* var blocksOrderedByAddress = blocksByStartAddress.Values.OrderBy(b => b.StartAddress).ToArray();
        if (blocksOrderedByAddress.Length == 0)
        {
            return;
        }

        for (int i = 0; i < blocksOrderedByAddress.Length - 1; i++)
        {
            blocksOrderedByAddress[i].EndAddress = blocksOrderedByAddress[i + 1].StartAddress;
            blocksOrderedByAddress[i].LastInstruction = script.FindInstructionAt(blocksOrderedByAddress[i].EndAddress)?.Previous;
        }

        var lastBlock = blocksOrderedByAddress[^1];
        var lastInstruction = script.FindInstructionAt(lastBlock.StartAddress);
        Debug.Assert(lastInstruction is not null);
        while (lastInstruction.Next is not null and not IREndOfScript) { lastInstruction = lastInstruction.Next; }
        lastBlock.EndAddress = lastInstruction.Address;
        lastBlock.LastInstruction = lastInstruction.Previous;*/
    }

    private void ConnectBlocks()
    {
        foreach (var block in blocksByStartAddress.Values)
        {
            if (block.IsEmpty)
            {
                continue;
            }

            var lastInstruction = block.LastInstruction;
            Debug.Assert(lastInstruction is not null);
            if (lastInstruction is IRJump j)
            {
                // an unconditional jump has a single successor, the jump target
                block.Successors = new[] { CreateEdge(block, GetBlock(j.JumpAddress)) };
            }
            else if (lastInstruction is IRJumpIfZero jz)
            {
                // StartBlock(jz.JumpAddress);
                // if (jz.Next is not null)
                // {
                //     StartBlock(jz.Next.Address);
                // }
                // a conditional jump has two successors, the jump target and the next instruction
                if (jz.Next is not null)
                {
                    Console.WriteLine($"IfFalse: {jz.JumpAddress:000000} ; IfTrue: {jz.Next.Address:000000}");
                    block.Successors = new[] { CreateIfFalseEdge(block, GetBlock(jz.JumpAddress)), CreateIfTrueEdge(block, GetBlock(jz.Next.Address)) };
                }
                else
                {
                    block.Successors = new[] { CreateIfFalseEdge(block, GetBlock(jz.JumpAddress)) };
                }
            }
            else if (lastInstruction is IRSwitch sw)
            {
                // a switch has as successors all the cases jump targets and the next instruction (default case)
                block.Successors = new CFGEdgeInProgress[sw.Cases.Length + 1];

                for (int i = 0; i < sw.Cases.Length; i++)
                {
                    var c = sw.Cases[i];
                    block.Successors[i] = CreateSwitchCaseEdge(block, GetBlock(c.JumpAddress), c.Value);
                }
                Debug.Assert(lastInstruction.Next is not null, "There should always be an instruction after a switch");
                block.Successors[^1] = CreateSwitchCaseEdge(block, GetBlock(lastInstruction.Next!.Address), value: null);
            }
            else if (lastInstruction is IRLeave)
            {
                // a LEAVE instruction has no successors
                block.Successors = Array.Empty<CFGEdgeInProgress>();
            }
            else
            {
                // for everything else, the only successor is the next instruction
                var nextInstruction = lastInstruction.Next;
                if (nextInstruction is not null)
                {
                    block.Successors = new[] { CreateEdge(block, GetBlock(nextInstruction.Address)) };
                }
            }
        }
    }

    private void RemoveUnreachableBlocks()
    {
        // Remove blocks that are not successors of any other block, except for the first block
        // which is the function entrypoint.
        // These are normally caused by the J after LEAVE that the R* compilers likes to insert
        // TODO: investigate, are these J after LEAVE part of if-else with returns inside? e.g.:
        //
        //  FUNC INT GET_SOMETHING()
        //      IF TRUE
        //          RETURN 1
        //      ELSE
        //          RETURN 2
        //      ENDIF
        //  ENDFUNC
        //

        var reachableBlocks = new HashSet<CFGBlockInProgress>(capacity: blocksByStartAddress.Count);
        var blocksToVisit = new Stack<CFGBlockInProgress>(capacity: blocksByStartAddress.Count);
        var firstBlock = blocksByStartAddress[start.Address];

        blocksToVisit.Push(firstBlock);
        while (blocksToVisit.TryPop(out var block))
        {
            reachableBlocks.Add(block);

            foreach (var succ in block.Successors)
            {
                if (!reachableBlocks.Contains(succ.To))
                {
                    blocksToVisit.Push(succ.To);
                }
            }
        }

        foreach (var (startAddress, block) in blocksByStartAddress.ToArray()) // create a copy to be able to remove blocks from the dictionary inside the foreach
        {
            if (!reachableBlocks.Contains(block))
            {
                blocksByStartAddress.Remove(startAddress);
            }
        }
    }

    private CFGBlockInProgress GetBlock(int address) => blocksByStartAddress[address];

    private void StartBlock(int startAddress)
    {
        if (!blocksByStartAddress.ContainsKey(startAddress))
        {
            var newBlock = new CFGBlockInProgress
            {
                StartAddress = startAddress,
                FirstInstruction = code.FindInstructionAt(startAddress)
            };
            blocksByStartAddress.Add(startAddress, newBlock);
        }
    }

    private CFGEdgeInProgress CreateEdge(CFGBlockInProgress from, CFGBlockInProgress to)
        => new()
        {
            From = from,
            To = to,
            Kind = CFGEdgeKind.Unconditional,
        };

    private CFGEdgeInProgress CreateSwitchCaseEdge(CFGBlockInProgress from, CFGBlockInProgress to, int? value)
        => new()
        {
            From = from,
            To = to,
            Kind = CFGEdgeKind.SwitchCase,
            SwitchCaseValue = value,
        };

    private CFGEdgeInProgress CreateIfFalseEdge(CFGBlockInProgress from, CFGBlockInProgress to)
        => new()
        {
            From = from,
            To = to,
            Kind = CFGEdgeKind.IfFalse,
        };

    private CFGEdgeInProgress CreateIfTrueEdge(CFGBlockInProgress from, CFGBlockInProgress to)
        => new()
        {
            From = from,
            To = to,
            Kind = CFGEdgeKind.IfTrue,
        };
}

namespace ScTools.Decompilation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ScTools.Decompilation.Intermediate;

    public class ControlFlowGraphBuilder
    {
        private readonly Function function;
        private readonly Dictionary<int, ControlFlowBlock> blocksByStartAddress = new();

        public ControlFlowGraphBuilder(Function function)
        {
            this.function = function;
        }

        public ControlFlowGraph BuildGraph()
        {
            StartBlocks();
            EndBlocks();
            ConnectBlocks();
            RemoveUnreachableBlocks();

            var cfg = new ControlFlowGraph(function, blocksByStartAddress.Values);
            return cfg;
        }

        private void StartBlocks()
        {
            blocksByStartAddress.Clear();

            foreach (var inst in function.EnumerateInstructions())
            {
                if (inst.Address == function.StartAddress)
                {
                    StartBlock(inst.Address);
                }

                if (inst.Opcode is Opcode.J or Opcode.JZ)
                {
                    // start a new block at the target and at the next instruction
                    StartBlock(inst.GetJumpAddress());
                    StartBlock(inst.Next().Address);
                }
                else if (inst.Opcode is Opcode.SWITCH)
                {
                    var caseCount = inst.GetSwitchCaseCount();
                    // start a new block at each case
                    for (int i = 0; i < caseCount; i++)
                    {
                        StartBlock(inst.GetSwitchCase(i).JumpAddress);
                    }

                    // start a new block after the switch instruction (normally a jump to the default case)
                    StartBlock(inst.Next().Address);
                }
                else if (inst.Opcode is Opcode.LEAVE)
                {
                    // start a new block after a return if it is not at the end of the function
                    var next = inst.Next();
                    if (next.Address < function.EndAddress)
                    {
                        StartBlock(next.Address);
                    }
                }
            }
        }

        private void EndBlocks()
        {
            var blocksOrderedByAddress = blocksByStartAddress.Values.OrderBy(b => b.StartAddress).ToArray();
            if (blocksOrderedByAddress.Length == 0)
            {
                return;
            }

            for (int i = 0; i < blocksOrderedByAddress.Length - 1; i++)
            {
                blocksOrderedByAddress[i].EndAddress = blocksOrderedByAddress[i + 1].StartAddress;
            }

            blocksOrderedByAddress.Last().EndAddress = function.EndAddress;
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
                if (lastInstruction.Opcode is Opcode.J)
                {
                    // an unconditional jump has a single successor, the jump target
                    block.Successors = new[] { CreateEdge(block, GetBlock(lastInstruction.GetJumpAddress())) };
                }
                else if (lastInstruction.Opcode is Opcode.JZ)
                {
                    // a conditional jump has two successors, the jump target and the next instruction
                    var nextInstruction = lastInstruction.Next();
                    if (nextInstruction && nextInstruction.Address < function.EndAddress)
                    {
                        block.Successors = new[] { CreateIfFalseEdge(block, GetBlock(lastInstruction.GetJumpAddress())), CreateIfTrueEdge(block, GetBlock(nextInstruction.Address)) };
                    }
                    else
                    {
                        block.Successors = new[] { CreateIfFalseEdge(block, GetBlock(lastInstruction.GetJumpAddress())) };
                    }
                }
                else if (lastInstruction.Opcode is Opcode.SWITCH)
                {
                    // a switch has as successors all the cases jump targets and the next instruction (default case)
                    var caseCount = lastInstruction.GetSwitchCaseCount();
                    block.Successors = new ControlFlowEdge[caseCount + 1];

                    for (int i = 0; i < caseCount; i++)
                    {
                        var (caseValue, caseJumpAddress) = lastInstruction.GetSwitchCase(i);
                        block.Successors[i] = CreateSwitchCaseEdge(block, GetBlock(caseJumpAddress), caseValue);
                    }
                    block.Successors[caseCount] = CreateSwitchCaseEdge(block, GetBlock(lastInstruction.Next().Address), value: null);
                }
                else if (lastInstruction.Opcode is Opcode.LEAVE)
                {
                    // a LEAVE instruction has no successors
                    block.Successors = Array.Empty<ControlFlowEdge>();
                }
                else
                {
                    // for everything else, the only successor is the next instruction
                    var nextInstruction = lastInstruction.Next();
                    if (nextInstruction && nextInstruction.Address < function.EndAddress)
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



            var reachableBlocks = new HashSet<ControlFlowBlock>(capacity: blocksByStartAddress.Count);
            var blocksToVisit = new Stack<ControlFlowBlock>(capacity: blocksByStartAddress.Count);
            var firstBlock = blocksByStartAddress.Values.OrderBy(b => b.StartAddress).First();

            blocksToVisit.Push(firstBlock);
            while (blocksToVisit.Count > 0)
            {
                var block = blocksToVisit.Pop();
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

        private ControlFlowBlock GetBlock(int address) => blocksByStartAddress[address];

        private void StartBlock(int address)
        {
            if (!blocksByStartAddress.ContainsKey(address))
            {
                blocksByStartAddress.Add(address, new ControlFlowBlock(function) { StartAddress = address, EndAddress = -1 });
            }
        }

        private ControlFlowEdge CreateEdge(ControlFlowBlock from, ControlFlowBlock to)
            => new()
            {
                From = from,
                To = to,
                Kind = ControlFlowEdgeKind.Unconditional,
            };

        private ControlFlowEdge CreateSwitchCaseEdge(ControlFlowBlock from, ControlFlowBlock to, int? value)
            => new()
            {
                From = from,
                To = to,
                Kind = ControlFlowEdgeKind.SwitchCase,
                SwitchCaseValue = value,
            };

        private ControlFlowEdge CreateIfFalseEdge(ControlFlowBlock from, ControlFlowBlock to)
            => new()
            {
                From = from,
                To = to,
                Kind = ControlFlowEdgeKind.IfFalse,
            };

        private ControlFlowEdge CreateIfTrueEdge(ControlFlowBlock from, ControlFlowBlock to)
            => new()
            {
                From = from,
                To = to,
                Kind = ControlFlowEdgeKind.IfTrue,
            };
    }
}

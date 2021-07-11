namespace ScTools.Decompilation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ScTools.ScriptAssembly;

    public class ControlFlowGraphBuilder
    {
        private readonly Function function;
        private readonly Dictionary<int, CFGBlock> blocksByStartAddress = new();

        public ControlFlowGraphBuilder(Function function)
        {
            this.function = function;
        }

        public ControlFlowGraph BuildGraph()
        {
            StartBlocks();
            EndBlocks();
            ConnectBlocks();

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

                if (inst.Opcode.IsJump())
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
                    block.Successors = new[] { GetBlock(lastInstruction.GetJumpAddress()) };
                }
                else if (lastInstruction.Opcode.IsJump())
                {
                    // a conditional jump has two successors, the jump target and the next instruction
                    var nextInstruction = lastInstruction.Next();
                    if (nextInstruction && nextInstruction.Address < function.EndAddress)
                    {
                        block.Successors = new[] { GetBlock(lastInstruction.GetJumpAddress()), GetBlock(nextInstruction.Address) };
                    }
                    else
                    {
                        block.Successors = new[] { GetBlock(lastInstruction.GetJumpAddress()) };
                    }
                }
                else if (lastInstruction.Opcode is Opcode.SWITCH)
                {
                    // a switch has as successors all the cases jump targets and the next instruction (default case)
                    var caseCount = lastInstruction.GetSwitchCaseCount();
                    block.Successors = new CFGBlock[caseCount + 1];

                    for (int i = 0; i < caseCount; i++)
                    {
                        block.Successors[i] = GetBlock(lastInstruction.GetSwitchCase(i).JumpAddress);
                    }
                    block.Successors[caseCount] = GetBlock(lastInstruction.Next().Address);
                }
                else if (lastInstruction.Opcode is Opcode.LEAVE)
                {
                    // a LEAVE instruction has no successors
                    block.Successors = Array.Empty<CFGBlock>();
                }
                else
                {
                    // for everything else, the only successor is the next instruction
                    var nextInstruction = lastInstruction.Next();
                    if (nextInstruction && nextInstruction.Address < function.EndAddress)
                    {
                        block.Successors = new[] { GetBlock(nextInstruction.Address) };
                    }
                }
            }
        }

        private CFGBlock GetBlock(int address) => blocksByStartAddress[address];

        private void StartBlock(int address)
        {
            if (!blocksByStartAddress.ContainsKey(address))
            {
                blocksByStartAddress.Add(address, new CFGBlock(function) { StartAddress = address, EndAddress = -1 });
            }
        }
    }
}

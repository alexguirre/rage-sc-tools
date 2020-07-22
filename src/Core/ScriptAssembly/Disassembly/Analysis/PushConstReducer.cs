namespace ScTools.ScriptAssembly.Disassembly.Analysis
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Combines multiple <c>PUSH_CONST</c> instructions into a single one.
    /// </summary>
    public sealed class PushConstReducer : BaseLocationVisitor
    {
        private readonly List<Operand> operands = new List<Operand>(16);

        public override Location VisitHLInstruction(HLInstructionLocation loc, VisitContext context)
        {
            if (loc.InstructionId != HighLevelInstruction.UniqueId.PUSH_CONST)
            {
                return loc;
            }

            operands.Clear();

            int locCount = 0;
            Location curr = loc;
            do
            {
                if (curr is HLInstructionLocation h)
                {
                    operands.AddRange(h.Operands);
                }
                locCount++;
                curr = curr.Next;
            } while (curr != null &&
                     curr.Label == null &&
                     (curr is EmptyLocation || (curr is HLInstructionLocation hl && hl.InstructionId == HighLevelInstruction.UniqueId.PUSH_CONST)));

            if (locCount == 1) // only the original location
            {
                return loc;
            }

            var newLoc = new HLInstructionLocation(loc.IP, HighLevelInstruction.UniqueId.PUSH_CONST)
            {
                Label = loc.Label,
                Operands = operands.ToArray(),
            };

            // append empty locations to replace the original PUSH_CONSTs
            Location currNewLoc = newLoc;
            for (int i = 0; i < locCount - 1; i++)
            {
                currNewLoc.Next = new EmptyLocation(newLoc.IP, null)
                {
                    Previous = currNewLoc
                };
                currNewLoc = currNewLoc.Next;
            }

            return newLoc;
        }
    }
}

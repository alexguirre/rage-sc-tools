namespace ScTools.ScriptAssembly.Disassembly.Analysis
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Converts pairs of <c>PUSH_CONST_*</c> and <c>STRING</c> instructions to high-level `<c>PUSH_CONST "theString"</c>` instructions.
    /// </summary>
    public sealed class PushStringSimplifier : BaseLocationVisitor
    {
        public override Location VisitInstruction(InstructionLocation loc, VisitContext context)
        {
            if (loc.Opcode != Opcode.STRING)
            {
                return loc;
            }

            // go back until we find the push instruction
            InstructionLocation prevLoc = null;
            bool found = false;
            int locCount = 0; // how many location we will replace before STRING
            while ((prevLoc = (prevLoc ?? loc).PreviousInstruction()) != null)
            {
                locCount++;
                if (prevLoc.Opcode == Opcode.NOP)
                {
                    Debug.Assert(prevLoc.Label == null, "There shouldn't be any jump pointing here");
                    continue;
                }

                if (prevLoc.Opcode == Opcode.J && // is there a jump to the STRING instruction
                    context.Function.GetLabelIP(prevLoc.Operands[0].Identifier) == loc.IP)
                {
                    Debug.Assert(prevLoc.Label == null, "There shouldn't be any jump pointing here");
                    continue;
                }

                if (IsPush(prevLoc.Opcode))
                {
                    found = true;
                }
                else
                {
                    found = false;
                }
                break;
            }

            if (!found)
            {
                throw new NotImplementedException("This should be impossible (at least in vanilla scripts). All STRING instructions should be preceded by a PUSH_CONST_* instruction.");
            }

            uint strId = GetPushValue(prevLoc);

            uint ip = prevLoc.IP;
            string label = prevLoc.Label;

            // in instructions that push multiple values we only take the last one,
            // so we need to insert new instructions that push the other values
            Location newPrevLoc = null;
            if (prevLoc.Opcode == Opcode.PUSH_CONST_U8_U8_U8)
            {
                newPrevLoc = new InstructionLocation(ip, Opcode.PUSH_CONST_U8_U8)
                {
                    Label = label,
                    Operands = new[] { prevLoc.Operands[0], prevLoc.Operands[1] }
                };
                ip += Instruction.SizeOf(Opcode.PUSH_CONST_U8_U8);
                label = null;
                locCount--;
            }
            else if (prevLoc.Opcode == Opcode.PUSH_CONST_U8_U8)
            {
                newPrevLoc = new InstructionLocation(ip, Opcode.PUSH_CONST_U8)
                {
                    Label = label,
                    Operands = new[] { prevLoc.Operands[0] }
                };
                ip += Instruction.SizeOf(Opcode.PUSH_CONST_U8);
                label = null;
                locCount--;
            }

            // the new high-level instruction
            Location newStartLoc = new HLInstructionLocation(ip, HighLevelInstruction.UniqueId.PUSH_CONST)
            {
                Label = label,
                Operands = new[] { new Operand(context.Disassembly.Script.String(strId), OperandType.String) }
            };
            Location newEndLoc = newStartLoc;

            if (newPrevLoc != null)
            {
                // if we have a newPrevLoc, prepend it to the new location
                newStartLoc = newPrevLoc;
                newStartLoc.Next = newEndLoc;
                newEndLoc.Previous = newStartLoc;
            }

            // prepend empty location to replace the original location (NOPs, J and PUSH_*)
            for (int i = 0; i < locCount; i++)
            {
                var empty = new EmptyLocation(newStartLoc.IP, null);
                newStartLoc.Previous = empty;
                empty.Next = newStartLoc;
                newStartLoc = empty;
            }

            // return the new locations
            return newEndLoc;
        }

        private static bool IsPush(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.PUSH_CONST_0:
                case Opcode.PUSH_CONST_1:
                case Opcode.PUSH_CONST_2:
                case Opcode.PUSH_CONST_3:
                case Opcode.PUSH_CONST_4:
                case Opcode.PUSH_CONST_5:
                case Opcode.PUSH_CONST_6:
                case Opcode.PUSH_CONST_7:
                case Opcode.PUSH_CONST_S16:
                case Opcode.PUSH_CONST_U24:
                case Opcode.PUSH_CONST_U32:
                case Opcode.PUSH_CONST_U8:
                case Opcode.PUSH_CONST_U8_U8:
                case Opcode.PUSH_CONST_U8_U8_U8:
                    return true;
                default:
                    return false;
            }
        }

        private static uint GetPushValue(InstructionLocation loc)
        {
            Debug.Assert(IsPush(loc.Opcode));

            return loc.Opcode switch
            {
                Opcode.PUSH_CONST_0 => 0,
                Opcode.PUSH_CONST_1 => 1,
                Opcode.PUSH_CONST_2 => 2,
                Opcode.PUSH_CONST_3 => 3,
                Opcode.PUSH_CONST_4 => 4,
                Opcode.PUSH_CONST_5 => 5,
                Opcode.PUSH_CONST_6 => 6,
                Opcode.PUSH_CONST_7 => 7,
                Opcode.PUSH_CONST_S16 => loc.Operands[0].U32,
                Opcode.PUSH_CONST_U24 => loc.Operands[0].U32,
                Opcode.PUSH_CONST_U32 => loc.Operands[0].U32,
                Opcode.PUSH_CONST_U8 => loc.Operands[0].U32,
                Opcode.PUSH_CONST_U8_U8 => loc.Operands[1].U32,
                Opcode.PUSH_CONST_U8_U8_U8 => loc.Operands[2].U32,
                _ => throw new InvalidOperationException()
            };
        }
    }
}

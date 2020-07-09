namespace ScTools.ScriptAssembly.Disassembly.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using ScTools.GameFiles;

    /// <summary>
    /// Converts pairs of <c>PUSH_CONST_*</c> and <c>STRING</c> instructions to high-level <c>PUSH_STRING</c> instructions.
    /// </summary>
    public class PushStringAnalyzer
    {
        public Script Script { get; }

        public PushStringAnalyzer(Script sc)
        {
            Script = sc ?? throw new ArgumentNullException(nameof(sc));
        }

        public void Analyze(Function function)
        {
            for (int i = 0; i < function.Code.Count; i++)
            {
                Location loc = function.Code[i];

                if (loc.HasInstruction && loc.Opcode == Opcode.STRING)
                {
                    // go back until we find the push instruction
                    int k = i;
                    Location prevLoc = default;
                    bool found = false;
                    while (--k >= 0)
                    {
                        prevLoc = function.Code[k];

                        if (!prevLoc.HasInstruction || prevLoc.Opcode == Opcode.NOP)
                        {
                            continue;
                        }

                        if (prevLoc.Opcode == Opcode.J && // is there a jump to the STRING instruction
                            prevLoc.Operands[0].AsS16() == (loc.IP - (prevLoc.IP + 3)))
                        {
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

                    if (found)
                    {
                        uint id = GetPushValue(prevLoc);

                        uint ip = prevLoc.IP;
                        string label = prevLoc.Label;
                        List<Location> newLoc = new List<Location>(2);

                        // in instructions that push multiple values we only take the last one,
                        // so we need to insert new instructions that push the other values
                        if (prevLoc.Opcode == Opcode.PUSH_CONST_U8_U8_U8)
                        {
                            newLoc.Add(new Location(ip, Opcode.PUSH_CONST_U8_U8)
                            {
                                Label = label,
                                Operands = new[] { prevLoc.Operands[0], prevLoc.Operands[1] }
                            });
                            ip += Instruction.SizeOf(Opcode.PUSH_CONST_U8_U8);
                            label = null;
                        }
                        else if (prevLoc.Opcode == Opcode.PUSH_CONST_U8_U8)
                        {
                            newLoc.Add(new Location(ip, Opcode.PUSH_CONST_U8)
                            {
                                Label = label,
                                Operands = new[] { prevLoc.Operands[0] }
                            });
                            ip += Instruction.SizeOf(Opcode.PUSH_CONST_U8);
                            label = null;
                        }

                        // the new high-level instruction
                        newLoc.Add(new Location(ip, HighLevelInstruction.PUSH_STRING.Index)
                        {
                            Label = label,
                            Operands = new[] { new Operand(Script.String(id), OperandType.String) }
                        });

                        // remove the old instructions...
                        function.Code.RemoveRange(k, i - k + 1);
                        // ... and insert the new ones
                        function.Code.InsertRange(k, newLoc);

                        // set the new index to the first instruction after the new ones
                        i = k + newLoc.Count;
                    }
                    else
                    {
                        Debug.Assert(false, "This should be impossible (at least in vanilla scripts). All STRING instructions should be preceded by a PUSH_CONST_* instruction.");
                    }
                }
            }
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

        private static uint GetPushValue(Location loc)
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

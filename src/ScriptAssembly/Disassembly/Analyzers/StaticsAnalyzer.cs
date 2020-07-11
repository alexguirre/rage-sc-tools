namespace ScTools.ScriptAssembly.Disassembly.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using ScTools.ScriptAssembly.Types;

    public class StaticsAnalyzer
    {
        public DisassembledScript Disassembly { get; }
        
        public StaticsAnalyzer(DisassembledScript disassembly)
        {
            Disassembly = disassembly ?? throw new ArgumentNullException(nameof(disassembly));
        }

        public void Analyze(Function function)
        {
            for (int i = 0; i < function.Code.Count; i++)
            {
                Location loc = function.Code[i];

                if (loc.HasInstruction && IsStatic(loc.Opcode))
                {
                    uint staticOffset = GetStaticOffset(loc);
                    Static s = GetStatic(staticOffset);
                    Location newLoc = new Location(loc.IP, GetHLReplacement(loc.Opcode)) { Label = loc.Label, Operands = new[] { new Operand(s.Name, OperandType.Identifier) } };
                    function.Code[i] = newLoc;
                }
            }
        }

        private Static GetStatic(uint offset)
        {
            bool isArg = offset >= (Disassembly.Args.FirstOrDefault()?.Offset ?? uint.MaxValue);

            if (isArg)
            {
                int i = Disassembly.Args.FindIndex(a => a.Offset == offset);
                Debug.Assert(i != -1);
                return Disassembly.Args[i];
            }
            else
            {
                int i = Disassembly.Statics.FindIndex(a => a.Offset == offset);
                Debug.Assert(i != -1);
                return Disassembly.Statics[i];
            }
        }

        private static bool IsStatic(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.STATIC_U8:
                case Opcode.STATIC_U8_LOAD:
                case Opcode.STATIC_U8_STORE:
                case Opcode.STATIC_U16:
                case Opcode.STATIC_U16_LOAD:
                case Opcode.STATIC_U16_STORE:
                    return true;
                default:
                    return false;
            }
        }

        private static HighLevelInstruction.UniqueId GetHLReplacement(Opcode opcode) => opcode switch
        {
            Opcode.STATIC_U8        => HighLevelInstruction.UniqueId.STATIC,
            Opcode.STATIC_U8_LOAD   => HighLevelInstruction.UniqueId.STATIC_LOAD,
            Opcode.STATIC_U8_STORE  => HighLevelInstruction.UniqueId.STATIC_STORE,
            Opcode.STATIC_U16       => HighLevelInstruction.UniqueId.STATIC,
            Opcode.STATIC_U16_LOAD  => HighLevelInstruction.UniqueId.STATIC_LOAD,
            Opcode.STATIC_U16_STORE => HighLevelInstruction.UniqueId.STATIC_STORE,
            _ => throw new ArgumentException("Not a STATIC opcode", nameof(opcode)),
        };

        private static uint GetStaticOffset(Location loc)
        {
            Debug.Assert(IsStatic(loc.Opcode));

            return loc.Operands[0].U32;
        }
    }
}

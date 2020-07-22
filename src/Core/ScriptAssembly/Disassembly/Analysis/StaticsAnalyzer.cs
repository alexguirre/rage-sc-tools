namespace ScTools.ScriptAssembly.Disassembly.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using ScTools.ScriptAssembly.Types;

    public class StaticsAnalyzer // TODO: reimplement StaticsAnalyzer with ILocationVisitor if possible
    {
        public DisassembledScript Disassembly { get; }
        
        public StaticsAnalyzer(DisassembledScript disassembly)
        {
            Disassembly = disassembly ?? throw new ArgumentNullException(nameof(disassembly));
        }

        public void Analyze(Function function)
        {
            foreach (var loc in function.CodeStart.EnumerateForward())
            {
                if (loc is InstructionLocation iloc && IsStatic(iloc.Opcode))
                {
                    uint staticOffset = GetStaticOffset(iloc);
                    Static s = GetStatic(staticOffset);
                    var newLoc = new HLInstructionLocation(loc.IP, GetHLReplacement(iloc.Opcode)) { Label = loc.Label, Operands = new[] { new Operand(s.Name, OperandType.Identifier) } };
                    
                    // replace loc with newLoc
                    loc.Previous.Next = newLoc;
                    newLoc.Previous = loc.Previous;
                    loc.Next.Previous = newLoc;
                    newLoc.Next = loc.Next;
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

        private static uint GetStaticOffset(InstructionLocation loc)
        {
            Debug.Assert(IsStatic(loc.Opcode));

            return loc.Operands[0].U32;
        }
    }
}

namespace ScTools.ScriptAssembly.Disassembly.Analysis
{
    /// <summary>
    /// Converts <c>PUSH_CONST_*</c> instructions to high-level <c>PUSH_CONST</c> instructions.
    /// </summary>
    public sealed class PushConstSimplifier : BaseLocationVisitor
    {
        public override Location VisitInstruction(InstructionLocation loc, VisitContext context)
        {
            var newLoc = loc.Opcode switch
            {
                Opcode.PUSH_CONST_M1 => NewPushConstU(loc.IP, unchecked((uint)-1)), // TODO: represent as signed integer
                Opcode.PUSH_CONST_0 => NewPushConstU(loc.IP, 0),
                Opcode.PUSH_CONST_1 => NewPushConstU(loc.IP, 1),
                Opcode.PUSH_CONST_2 => NewPushConstU(loc.IP, 2),
                Opcode.PUSH_CONST_3 => NewPushConstU(loc.IP, 3),
                Opcode.PUSH_CONST_4 => NewPushConstU(loc.IP, 4),
                Opcode.PUSH_CONST_5 => NewPushConstU(loc.IP, 5),
                Opcode.PUSH_CONST_6 => NewPushConstU(loc.IP, 6),
                Opcode.PUSH_CONST_7 => NewPushConstU(loc.IP, 7),
                Opcode.PUSH_CONST_FM1 => NewPushConstF(loc.IP, -1.0f),
                Opcode.PUSH_CONST_F0 => NewPushConstF(loc.IP, 0.0f),
                Opcode.PUSH_CONST_F1 => NewPushConstF(loc.IP, 1.0f),
                Opcode.PUSH_CONST_F2 => NewPushConstF(loc.IP, 2.0f),
                Opcode.PUSH_CONST_F3 => NewPushConstF(loc.IP, 3.0f),
                Opcode.PUSH_CONST_F4 => NewPushConstF(loc.IP, 4.0f),
                Opcode.PUSH_CONST_F5 => NewPushConstF(loc.IP, 5.0f),
                Opcode.PUSH_CONST_F6 => NewPushConstF(loc.IP, 6.0f),
                Opcode.PUSH_CONST_F7 => NewPushConstF(loc.IP, 7.0f),
                Opcode.PUSH_CONST_U32 => NewPushConstU(loc.IP, loc.Operands[0].U32),
                Opcode.PUSH_CONST_F => NewPushConstF(loc.IP, loc.Operands[0].F32),
                Opcode.PUSH_CONST_U8 => NewPushConstU(loc.IP, loc.Operands[0].AsU8()),
                Opcode.PUSH_CONST_U8_U8 => NewPushConstUU(loc.IP, loc.Operands[0].AsU8(), loc.Operands[1].AsU8()),
                Opcode.PUSH_CONST_U8_U8_U8 => NewPushConstUUU(loc.IP, loc.Operands[0].AsU8(), loc.Operands[1].AsU8(), loc.Operands[2].AsU8()),
                Opcode.PUSH_CONST_S16 => NewPushConstU(loc.IP, loc.Operands[0].AsU16()), // TODO: represent as signed integer
                Opcode.PUSH_CONST_U24 => NewPushConstU24(loc.IP, loc.Operands[0].AsU24(), context),
                _ => loc
            };

            newLoc.Label = loc.Label;
            return newLoc;

            static Location NewPushConstU(uint ip, uint value)
                => new HLInstructionLocation(ip, HighLevelInstruction.UniqueId.PUSH_CONST) { Operands = new[] { new Operand(value) } };

            static Location NewPushConstUU(uint ip, uint value1, uint value2)
                => new HLInstructionLocation(ip, HighLevelInstruction.UniqueId.PUSH_CONST) { Operands = new[] { new Operand(value1), new Operand(value2) } };

            static Location NewPushConstUUU(uint ip, uint value1, uint value2, uint value3)
                => new HLInstructionLocation(ip, HighLevelInstruction.UniqueId.PUSH_CONST) { Operands = new[] { new Operand(value1), new Operand(value2), new Operand(value3) } };

            static Location NewPushConstF(uint ip, float value)
                => new HLInstructionLocation(ip, HighLevelInstruction.UniqueId.PUSH_CONST) { Operands = new[] { new Operand(value) } };

            static Location NewPushConstU24(uint ip, uint value, VisitContext context)
            {
                //check if the value matches a function address
                foreach (var function in context.Disassembly.Functions)
                {
                    // TODO: this is not 100% accurate, is there anything we can do to know if this is indeed
                    //       a function address instead of just a constant with the same value?
                    if (function.StartIP == value)
                    {
                        return new HLInstructionLocation(ip, HighLevelInstruction.UniqueId.PUSH_CONST) { Operands = new[] { new Operand(function.Name, OperandType.AddrOfFunction) } };
                    }
                    else if (function.StartIP > value)
                    {
                        break;
                    }
                }

                return NewPushConstU(ip, value);
            }
        }
    }
}

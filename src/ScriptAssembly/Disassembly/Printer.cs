namespace ScTools.ScriptAssembly.Disassembly
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection.Metadata.Ecma335;

    public static class Printer
    {
        public static string PrintOperand(Operand operand)
            => operand.Type switch
            {
                OperandType.U32 => operand.U32.ToString(),
                OperandType.U64 => operand.U64.ToString(),
                OperandType.F32 => operand.F32.ToString("0.0#######"),
                OperandType.Identifier => operand.Identifier,
                OperandType.SwitchCase => $"{operand.SwitchCase.Value}:{operand.SwitchCase.Label}",
                OperandType.String => $"\"{operand.String.Escape()}\"",
                _ => throw new InvalidOperationException()
            };

        public static string PrintOperands(IEnumerable<Operand> operands)
            => string.Join(" ", operands.Select(PrintOperand));

        public static string PrintLocation(Location location)
        {
            string str = "";
            if (location.Label != null)
            {
                str += $"\t{location.Label}:";
            }

            if (location.HasInstruction || location.HasHLInstruction)
            {
                if (location.Label != null)
                {
                    str += '\n';
                }

                str += $"\t\t{(location.HasInstruction ? location.Opcode.Mnemonic() : HighLevelInstruction.Set[location.HLIndex].Mnemonic)}";
                if (location.Operands.Length > 0)
                {
                    str += ' ';
                    str += PrintOperands(location.Operands);
                }
            }

            return str;
        }

        public static string PrintFunction(Function function)
        {
            string str = $"FUNC NAKED {function.Name} BEGIN\n";
            str += string.Join('\n', function.Code.Select(PrintLocation));
            str += "\nEND";
            return str;
        }
    }
}

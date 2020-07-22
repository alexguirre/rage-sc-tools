namespace ScTools.ScriptAssembly.Disassembly
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection.Metadata.Ecma335;
    using ScTools.ScriptAssembly.Types;

    public static class Printer
    {
        public static string PrintOperand(Operand operand)
            => operand.Type switch
            {
                OperandType.U32 => operand.U32.ToString(), // TODO: check if U32 operand value matches any hashes
                OperandType.U64 => operand.U64.ToString(),
                OperandType.F32 => operand.F32.ToString("0.0#######"),
                OperandType.Identifier => operand.Identifier,
                OperandType.SwitchCase => $"{operand.SwitchCase.Value}:{operand.SwitchCase.Label}",
                OperandType.String => $"\"{operand.String.Escape()}\"",
                OperandType.AddrOfFunction => $"ADDROF({operand.AddrOfFunction})",
                _ => throw new NotImplementedException()
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

            if (location is InstructionLocation || location is HLInstructionLocation)
            {
                if (location.Label != null)
                {
                    str += '\n';
                }

                var iLoc = location as InstructionLocation;
                var hlLoc = location as HLInstructionLocation;

                var mnemonic = iLoc?.Opcode.Mnemonic() ?? HighLevelInstruction.Set[(byte)hlLoc.InstructionId].Mnemonic;
                var operands = iLoc?.Operands ?? hlLoc.Operands;

                str += $"\t\t{mnemonic}";
                if (operands.Length > 0)
                {
                    str += ' ';
                    str += PrintOperands(operands);
                }
            }

            return str;
        }

        public static string PrintFunction(Function function) => function.Naked ? PrintNakedFunction(function) : PrintNonNakedFunction(function);

        private static string PrintNakedFunction(Function function)
        {
            Debug.Assert(function.Naked);

            string str = $"FUNC NAKED {function.Name} BEGIN\n";
            str += string.Join('\n', function.CodeStart.EnumerateForward()
                                                       // skip empty locations that don't have a label
                                                       .Where(l => !(l is EmptyLocation e) || e.Label != null)
                                                       .Select(PrintLocation));
            str += "\nEND";
            return str;
        }

        private static string PrintNonNakedFunction(Function function)
        {
            Debug.Assert(!function.Naked);

            string str = $"FUNC {function.Name}";
            if ((function.Arguments?.Count ?? 0) > 0)
            {
                str += '(';
                str += string.Join(", ", function.Arguments.Select(PrintLocal));
                str += ')';
            }
            if (function.ReturnType != null)
            {
                str += $": {function.ReturnType.Name}";
            }
            if ((function.Locals?.Count ?? 0) > 0)
            {
                str += "\n\t";
                str += string.Join("\n\t", function.Locals.Select(PrintLocal));
                str += "\nBEGIN\n";
            }
            else
            {
                str += " BEGIN\n";
            }
            str += string.Join('\n', function.CodeStart.EnumerateForward().Select(PrintLocation));
            str += "\nEND";
            return str;
        }

        public static string PrintLocal(Local l) => $"{l.Name}: {l.Type.Name}";

        public static string PrintStructField(StructField f) => $"\t{f.Name}:\t{f.Type.Name}";

        public static string PrintStruct(StructType s)
        {
            string str = $"STRUCT {s.Name} BEGIN\n";
            str += string.Join('\n', s.Fields.Select(PrintStructField));
            str += "\nEND";
            return str;
        }

        public static string PrintStatic(Static s) => $"\t{s.Name}:\t{s.Type.Name}{(s.InitialValue != 0 ? $" = {s.InitialValue}" : "")}";

        public static string PrintStatics(IEnumerable<Static> statics)
        {
            string str = $"STATICS BEGIN\n";
            str += string.Join('\n', statics.Select(PrintStatic));
            str += "\nEND";
            return str;
        }
        public static string PrintArguments(IEnumerable<StaticArgument> args)
        {
            string str = $"ARGS BEGIN\n";
            str += string.Join('\n', args.Select(PrintStatic));
            str += "\nEND";
            return str;
        }
    }
}

namespace ScTools.ScriptAssembly.Disassembly.Analysis
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using CodeWalker.GameFiles;

    /// <summary>
    /// Converts <c>NATIVE</c> instructions to high-level <c>CALL_NATIVE</c> instructions.
    /// </summary>
    public sealed class NativeSimplifier : BaseLocationVisitor
    {
        private readonly NativeDB nativeDB;

        public NativeSimplifier()
        {
            // TODO: load nativeDB outside NativeSimplifier
            using var reader = new BinaryReader(File.OpenRead("natives.scndb"));
            nativeDB = NativeDB.Load(reader);
        }

        public override Location VisitInstruction(InstructionLocation loc, VisitContext context)
        {
            if (loc.Opcode != Opcode.NATIVE)
            {
                return loc;
            }

            byte argCount = loc.Operands[0].AsU8();
            byte returnCount = loc.Operands[1].AsU8();
            ushort nativeIndex = loc.Operands[2].AsU16();

            ulong nativeHash = context.Disassembly.Script.NativeHash(nativeIndex);
            var cmd = nativeDB.Natives.First(n => n.CurrentHash == nativeHash);

            bool specifyArgReturnCounts = argCount != cmd.ParameterCount || returnCount != cmd.ReturnValueCount;
            
            if (argCount != cmd.ParameterCount) { Console.WriteLine($"WARNING: {cmd.Name} has a different arg count (in disassembly: {argCount}, in nativeDB: {cmd.ParameterCount})"); }
            if (returnCount != cmd.ReturnValueCount) { Console.WriteLine($"WARNING: {cmd.Name} has a different return value count (in disassembly: {returnCount}, in nativeDB: {cmd.ReturnValueCount})"); }

            var operands = specifyArgReturnCounts ?
                            new[] { new Operand(cmd.Name, OperandType.Identifier), new Operand(argCount), new Operand(returnCount) } :
                            new[] { new Operand(cmd.Name, OperandType.Identifier) };

            return new HLInstructionLocation(loc.IP, HighLevelInstruction.UniqueId.CALL_NATIVE) { Label = loc.Label, Operands = operands };
        }
    }
}

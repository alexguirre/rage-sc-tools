namespace ScTools.ScriptAssembly.Disassembly.Analysis
{
    public sealed class NopRemover : BaseLocationVisitor
    {
        public override Location VisitInstruction(InstructionLocation location, VisitContext context)
        {
            if (location.Opcode == Opcode.NOP && location.Label == null)
            {
                return null;
            }

            return location;
        }
    }
}

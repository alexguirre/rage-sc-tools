using System;

namespace ScTools.ScriptAssembly.Disassembly.Analysis
{
    public readonly struct VisitContext
    {
        public DisassembledScript Disassembly { get; }
        public Function Function { get; }
    
        public VisitContext(DisassembledScript script, Function function)
        {
            Disassembly = script ?? throw new ArgumentNullException(nameof(script));
            Function = function ?? throw new ArgumentNullException(nameof(function));
        }
    }

    public interface ILocationVisitor
    {
        Location VisitInstruction(InstructionLocation location, VisitContext context);
        Location VisitHLInstruction(HLInstructionLocation location, VisitContext context);
        Location VisitEmpty(EmptyLocation location, VisitContext context);
    }

    public abstract class BaseLocationVisitor : ILocationVisitor
    {
        public virtual Location VisitInstruction(InstructionLocation location, VisitContext context) => location;
        public virtual Location VisitHLInstruction(HLInstructionLocation location, VisitContext context) => location;
        public virtual Location VisitEmpty(EmptyLocation location, VisitContext context) => location;
    }

    public static class LocationVisitorExtensions
    {
        public static Location Accept<T>(this T location, VisitContext context, ILocationVisitor visitor) where T : Location => location switch
        {
            InstructionLocation l => visitor.VisitInstruction(l, context),
            HLInstructionLocation l => visitor.VisitHLInstruction(l, context),
            EmptyLocation l => visitor.VisitEmpty(l, context),
            _ => throw new NotImplementedException()
        };
    }
}

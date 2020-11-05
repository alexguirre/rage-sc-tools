#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class BoundStatic : BoundNode
    {
        public VariableSymbol Var { get; }
        public BoundExpression? Initializer { get; }
    
        public BoundStatic(VariableSymbol var, BoundExpression? initializer)
        {
            Var = var;
            Initializer = initializer;
        }
    }
}

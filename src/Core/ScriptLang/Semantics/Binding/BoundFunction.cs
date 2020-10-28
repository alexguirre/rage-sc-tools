#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class BoundFunction : BoundNode
    {
        public FunctionSymbol Function { get; }
        public IList<BoundStatement> Body { get; } = new List<BoundStatement>();

        public BoundFunction(FunctionSymbol function)
        {
            Function = function;
        }
    }
}

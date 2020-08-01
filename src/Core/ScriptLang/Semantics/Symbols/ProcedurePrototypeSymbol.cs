#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;

    public sealed class ProcedurePrototypeSymbol : ISymbol
    {
        public ProcedurePrototypeStatement AstNode { get; }
        public string Name => AstNode.Name.Name;
        public Scope Scope { get; } // scope for the parameters

        public ProcedurePrototypeSymbol(ProcedurePrototypeStatement astNode, Scope scope)
        {
            Debug.Assert(!scope.IsRoot);

            AstNode = astNode;
            Scope = scope;
        }
    }
}

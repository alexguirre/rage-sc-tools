#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;

    public sealed class ProcedureSymbol : ISymbol
    {
        public ProcedureStatement AstNode { get; }
        public string Name => AstNode.Name.Name;
        public Scope Scope { get; }

        public ProcedureSymbol(ProcedureStatement astNode, Scope scope)
        {
            Debug.Assert(!scope.IsRoot);

            AstNode = astNode;
            Scope = scope;
        }
    }
}

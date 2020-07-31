#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;

    public sealed class StructSymbol : ISymbol
    {
        public StructStatement AstNode { get; }
        public string Name => AstNode.Name.Name;
        public Scope Scope { get; }

        public StructSymbol(StructStatement astNode, Scope scope)
        {
            Debug.Assert(!scope.IsRoot);

            AstNode = astNode;
            Scope = scope;
        }
    }
}

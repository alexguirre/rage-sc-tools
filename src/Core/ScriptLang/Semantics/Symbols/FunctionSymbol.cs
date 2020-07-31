#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;

    public sealed class FunctionSymbol : ISymbol
    {
        public FunctionStatement AstNode { get; }
        public string Name => AstNode.Name.Name;
        public Scope Scope { get; }

        public FunctionSymbol(FunctionStatement astNode, Scope scope)
        {
            Debug.Assert(!scope.IsRoot);

            AstNode = astNode;
            Scope = scope;
        }
    }
}

#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;

    public sealed class FunctionPrototypeSymbol : ISymbol
    {
        public FunctionPrototypeStatement AstNode { get; }
        public string Name => AstNode.Name.Name;
        public Scope Scope { get; } // scope for the parameters

        public FunctionPrototypeSymbol(FunctionPrototypeStatement astNode, Scope scope)
        {
            Debug.Assert(!scope.IsRoot);

            AstNode = astNode;
            Scope = scope;
        }
    }
}

#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using ScTools.ScriptLang.Ast;

    public sealed class ParameterSymbol : ISymbol
    {
        public VariableDeclaration AstNode { get; }
        public string Name => AstNode.Name.Name;

        public ParameterSymbol(VariableDeclaration astNode)
            => AstNode = astNode;
    }
}

#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using ScTools.ScriptLang.Ast;

    public sealed class LocalSymbol : ISymbol
    {
        public VariableDeclarationWithInitializer AstNode { get; }
        public string Name => AstNode.Declaration.Name.Name;

        public LocalSymbol(VariableDeclarationWithInitializer astNode)
            => AstNode = astNode;
    }
}

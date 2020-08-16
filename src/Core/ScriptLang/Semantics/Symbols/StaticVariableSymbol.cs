#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using ScTools.ScriptLang.Ast;

    public sealed class StaticVariableSymbol : ISymbol
    {
        public VariableDeclarationWithInitializer AstNode { get; }
        public string Name => AstNode.Declaration.Name.Name;

        public StaticVariableSymbol(VariableDeclarationWithInitializer astNode)
            => AstNode = astNode;
    }
}

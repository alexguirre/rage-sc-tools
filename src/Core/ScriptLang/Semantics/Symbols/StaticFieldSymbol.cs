#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using ScTools.ScriptLang.Ast;

    public sealed class StaticFieldSymbol : ISymbol
    {
        public VariableDeclarationWithInitializer AstNode { get; }
        public string Name => AstNode.Declaration.Name.Name;

        public StaticFieldSymbol(VariableDeclarationWithInitializer astNode)
            => AstNode = astNode;
    }
}

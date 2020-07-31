#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using ScTools.ScriptLang.Ast;

    public sealed class StructFieldSymbol : ISymbol
    {
        public VariableDeclarationWithInitializer AstNode { get; }
        public string Name => AstNode.Declaration.Name.Name;

        public StructFieldSymbol(VariableDeclarationWithInitializer astNode)
            => AstNode = astNode;
    }
}

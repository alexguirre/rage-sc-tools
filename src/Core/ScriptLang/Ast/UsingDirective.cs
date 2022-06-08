namespace ScTools.ScriptLang.Ast;

using System.Diagnostics;

public record struct UsingDirectiveSemantics(CompilationUnit? ImportedCompilationUnit);

public sealed partial class UsingDirective : BaseNode, ISemanticNode<UsingDirectiveSemantics>
{
    public Token PathToken => Tokens[1];
    public string Path => PathToken.GetStringLiteral();
    public UsingDirectiveSemantics Semantics { get; set; }

    public UsingDirective(Token usingKeyword, Token pathString)
        : base(OfTokens(usingKeyword, pathString), OfChildren())
    {
        Debug.Assert(usingKeyword.Kind is TokenKind.USING);
        Debug.Assert(pathString.Kind is TokenKind.String);
    }
}

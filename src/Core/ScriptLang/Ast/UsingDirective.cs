namespace ScTools.ScriptLang.Ast;

using System.Diagnostics;

public sealed partial class UsingDirective : BaseNode
{
    public string Path => Tokens[1].GetStringLiteral();

    public UsingDirective(Token usingKeyword, Token pathString)
        : base(OfTokens(usingKeyword, pathString), OfChildren())
    {
        Debug.Assert(usingKeyword.Kind is TokenKind.USING);
        Debug.Assert(pathString.Kind is TokenKind.String);
    }
}

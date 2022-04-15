namespace ScTools.ScriptLang.Ast;

using System.Diagnostics;

public sealed class UsingDirective : BaseNode
{
    public string Path => Tokens[1].GetStringLiteral();

    public UsingDirective(Token usingKeyword, Token pathString)
        : base(OfTokens(usingKeyword, pathString), OfChildren())
    {
        Debug.Assert(usingKeyword.Kind is TokenKind.USING);
        Debug.Assert(pathString.Kind is TokenKind.String);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

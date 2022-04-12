namespace ScTools.ScriptLang.Ast.Statements;

using System.Diagnostics;

public sealed class BreakStatement : BaseStatement
{
    public IBreakableStatement? EnclosingStatement { get; set; }

    public BreakStatement(Token breakToken) : base(OfTokens(breakToken), OfChildren())
        => Debug.Assert(breakToken.Kind is TokenKind.BREAK);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

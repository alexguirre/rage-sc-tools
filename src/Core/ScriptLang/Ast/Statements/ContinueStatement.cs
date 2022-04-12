namespace ScTools.ScriptLang.Ast.Statements;

using System.Diagnostics;

public sealed class ContinueStatement : BaseStatement
{
    public ILoopStatement? EnclosingLoop { get; set; }

    public ContinueStatement(Token continueToken) : base(OfTokens(continueToken), OfChildren())
        => Debug.Assert(continueToken.Kind is TokenKind.CONTINUE);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

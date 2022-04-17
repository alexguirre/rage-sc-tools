namespace ScTools.ScriptLang.Ast.Statements;

using System.Diagnostics;

public record struct ContinueStatementSemantics(ILoopStatement? EnclosingLoop);

public sealed class ContinueStatement : BaseStatement
{
    public ContinueStatementSemantics Semantics { get; set; }

    public ContinueStatement(Token continueToken, Label? label) : base(OfTokens(continueToken), OfChildren(), label)
        => Debug.Assert(continueToken.Kind is TokenKind.CONTINUE);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);
}

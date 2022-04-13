namespace ScTools.ScriptLang.Ast.Statements;

using System.Diagnostics;

public record struct BreakStatementSemantics(IBreakableStatement? EnclosingStatement);

public sealed class BreakStatement : BaseStatement, ISemanticNode<BreakStatementSemantics>
{
    public BreakStatementSemantics Semantics { get; set; }

    public BreakStatement(Token breakToken, Label? label) : base(OfTokens(breakToken), OfChildren(), label)
        => Debug.Assert(breakToken.Kind is TokenKind.BREAK);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

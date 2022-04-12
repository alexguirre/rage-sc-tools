namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed class NullExpression : BaseExpression
{
    public NullExpression(Token nullToken)
        : base(OfTokens(nullToken), OfChildren())
    {
        Debug.Assert(nullToken.Kind is TokenKind.Null);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

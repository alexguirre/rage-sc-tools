namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed class IntLiteralExpression : BaseExpression, ILiteralExpression<int>
{
    public int Value { get; }

    public IntLiteralExpression(Token intToken)
        : base(OfTokens(intToken), OfChildren())
    {
        Debug.Assert(intToken.Kind is TokenKind.Integer);
        Value = intToken.GetIntLiteral();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(IntLiteralExpression)} {{ {nameof(Value)} = {Value} }}";
}

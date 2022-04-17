namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed class FloatLiteralExpression : BaseExpression, ILiteralExpression<float>
{
    public float Value { get; }

    public FloatLiteralExpression(Token floatToken)
        : base(OfTokens(floatToken), OfChildren())
    {
        Debug.Assert(floatToken.Kind is TokenKind.Integer or TokenKind.Float);
        Value = floatToken.GetFloatLiteral();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(FloatLiteralExpression)} {{ {nameof(Value)} = {Value} }}";
}

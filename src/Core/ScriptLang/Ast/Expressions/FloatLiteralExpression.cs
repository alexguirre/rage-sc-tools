namespace ScTools.ScriptLang.Ast.Expressions;

public sealed class FloatLiteralExpression : BaseExpression, ILiteralExpression<float>
{
    public float Value { get; set; }

    public FloatLiteralExpression(Token floatToken) : base(floatToken)
    {
        System.Diagnostics.Debug.Assert(floatToken.Kind is TokenKind.Integer or TokenKind.Float);
        Value = floatToken.GetFloatLiteral();
    }
    public FloatLiteralExpression(SourceRange source, float value) : base(source)
        => Value = value;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(FloatLiteralExpression)} {{ {nameof(Value)} = {Value} }}";
}

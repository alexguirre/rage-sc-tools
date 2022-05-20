namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed partial class FloatLiteralExpression : BaseExpression, ILiteralExpression<float>
{
    public float Value { get; }

    public FloatLiteralExpression(Token floatToken)
        : base(OfTokens(floatToken), OfChildren())
    {
        Debug.Assert(floatToken.Kind is TokenKind.Integer or TokenKind.Float);
        Value = floatToken.GetFloatLiteral();
    }

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(FloatLiteralExpression)} {{ {nameof(Value)} = {Value} }}";
}

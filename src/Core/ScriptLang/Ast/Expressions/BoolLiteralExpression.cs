namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed class BoolLiteralExpression : BaseExpression, ILiteralExpression<bool>
{
    public bool Value { get; }

    public BoolLiteralExpression(Token boolToken)
        : base(OfTokens(boolToken), OfChildren())
    {
        Debug.Assert(boolToken.Kind is TokenKind.Boolean);
        Value = boolToken.GetBoolLiteral();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(BoolLiteralExpression)} {{ {nameof(Value)} = {(Value ? "TRUE" : "FALSE")} }}";
}

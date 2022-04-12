namespace ScTools.ScriptLang.Ast.Expressions;

public sealed class BoolLiteralExpression : BaseExpression, ILiteralExpression<bool>
{
    public bool Value { get; set; }

    public BoolLiteralExpression(Token boolToken) : base(boolToken)
    {
        System.Diagnostics.Debug.Assert(boolToken.Kind is TokenKind.Boolean);
        Value = boolToken.GetBoolLiteral();
    }
    public BoolLiteralExpression(SourceRange source, bool value) : base(source)
        => Value = value;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(BoolLiteralExpression)} {{ {nameof(Value)} = {(Value ? "TRUE" : "FALSE")} }}";
}

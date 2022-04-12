namespace ScTools.ScriptLang.Ast.Expressions;

public sealed class IntLiteralExpression : BaseExpression, ILiteralExpression<int>
{
    public int Value { get; set; }

    public IntLiteralExpression(Token intToken) : base(intToken)
    {
        System.Diagnostics.Debug.Assert(intToken.Kind is TokenKind.Integer);
        Value = intToken.GetIntLiteral();
    }
    public IntLiteralExpression(SourceRange source, int value) : base(source)
        => Value = value;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(IntLiteralExpression)} {{ {nameof(Value)} = {Value} }}";
}

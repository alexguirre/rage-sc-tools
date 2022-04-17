namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

/// <remarks>
/// The string is nullable for representing the value of <code>STRING s = NULL</code> as a literal.
/// </remarks> 
public sealed class StringLiteralExpression : BaseExpression, ILiteralExpression<string?>
{
    public string? Value { get; }

    public StringLiteralExpression(Token stringOrNullToken)
        : base(OfTokens(stringOrNullToken), OfChildren())
    {
        Debug.Assert(stringOrNullToken.Kind is TokenKind.String or TokenKind.Null);
        Value = stringOrNullToken.Kind is TokenKind.String ? stringOrNullToken.GetStringLiteral() : null;
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(StringLiteralExpression)} {{ {nameof(Value)} = {(Value is null ? "NULL" : $"'{Value.Escape()}'")} }}";
}

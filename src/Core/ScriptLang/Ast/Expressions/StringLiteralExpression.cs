namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

/// <remarks>
/// The string is nullable for representing the value of <code>STRING s = NULL</code> as a literal.
/// </remarks> 
public sealed partial class StringLiteralExpression : BaseExpression, ILiteralExpression<string?>
{
    public string Value { get; }

    public StringLiteralExpression(Token stringOrNullToken)
        : base(OfTokens(stringOrNullToken), OfChildren())
    {
        Debug.Assert(stringOrNullToken.Kind is TokenKind.String);
        Value = stringOrNullToken.GetStringLiteral();
    }

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(StringLiteralExpression)} {{ {nameof(Value)} = '{Value.Escape()}' }}";
}

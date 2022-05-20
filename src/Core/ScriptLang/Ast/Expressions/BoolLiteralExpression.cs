namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed partial class BoolLiteralExpression : BaseExpression, ILiteralExpression<bool>
{
    public bool Value { get; }

    public BoolLiteralExpression(Token boolToken)
        : base(OfTokens(boolToken), OfChildren())
    {
        Debug.Assert(boolToken.Kind is TokenKind.Boolean);
        Value = boolToken.GetBoolLiteral();
    }

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(BoolLiteralExpression)} {{ {nameof(Value)} = {(Value ? "TRUE" : "FALSE")} }}";
}

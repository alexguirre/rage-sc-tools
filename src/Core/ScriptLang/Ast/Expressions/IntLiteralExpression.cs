namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed partial class IntLiteralExpression : BaseExpression, ILiteralExpression<int>
{
    public int Value { get; }

    public IntLiteralExpression(Token intToken)
        : base(OfTokens(intToken), OfChildren())
    {
        Debug.Assert(intToken.Kind is TokenKind.Integer);
        Value = intToken.GetIntLiteral();
    }

    object? ILiteralExpression.Value => Value;

    public override string DebuggerDisplay =>
        $@"{nameof(IntLiteralExpression)} {{ {nameof(Value)} = {Value} }}";
}

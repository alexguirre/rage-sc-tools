namespace ScTools.ScriptLang.Ast.Expressions;

using System;
using System.Diagnostics;

public enum UnaryOperator
{
    Negate,
    LogicalNot,
}

public sealed partial class UnaryExpression : BaseExpression
{
    public UnaryOperator Operator { get; }
    public IExpression SubExpression => (IExpression)Children[0];

    public UnaryExpression(Token operatorToken, IExpression subExpression)
        : base(OfTokens(operatorToken), OfChildren(subExpression))
    {
        Debug.Assert(operatorToken.Kind is TokenKind.NOT or TokenKind.Minus);
        Operator = UnaryOperatorExtensions.FromToken(operatorToken.Kind);
    }

    public override string DebuggerDisplay =>
        $@"{nameof(UnaryExpression)} {{ {nameof(Operator)} = {Operator}, {nameof(SubExpression)} = {SubExpression.DebuggerDisplay} }}";
}

public static class UnaryOperatorExtensions
{
    public static string ToLexeme(this UnaryOperator op)
        => ToToken(op).GetCanonicalLexeme();

    public static TokenKind ToToken(this UnaryOperator op)
        => op switch
        {
            UnaryOperator.Negate => TokenKind.Minus,
            UnaryOperator.LogicalNot => TokenKind.NOT,
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        };

    public static UnaryOperator FromToken(TokenKind token)
        => token switch
        {
            TokenKind.Minus => UnaryOperator.Negate,
            TokenKind.NOT => UnaryOperator.LogicalNot,
            _ => throw new ArgumentException($"Unknown unary operator '{token}'", nameof(token)),
        };

    public static UnaryOperator FromToken(string token)
        => token.ToUpperInvariant() switch
        {
            "-" => UnaryOperator.Negate,
            "NOT" => UnaryOperator.LogicalNot,
            _ => throw new ArgumentException($"Unknown unary operator '{token}'", nameof(token)),
        };
}

namespace ScTools.ScriptLang.Ast.Expressions;

using System;
using System.Diagnostics;

public enum UnaryOperator
{
    Negate,
    LogicalNot,
}

public sealed class UnaryExpression : BaseExpression
{
    public UnaryOperator Operator { get; }
    public IExpression SubExpression => (IExpression)Children[0];

    public UnaryExpression(Token operatorToken, IExpression subExpression)
        : base(OfTokens(operatorToken), OfChildren(subExpression))
    {
        Debug.Assert(operatorToken.Kind is TokenKind.NOT or TokenKind.Minus);
        Operator = UnaryOperatorExtensions.FromToken(operatorToken.Kind);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);

    public override string DebuggerDisplay =>
        $@"{nameof(UnaryExpression)} {{ {nameof(Operator)} = {Operator}, {nameof(SubExpression)} = {SubExpression.DebuggerDisplay} }}";
}

public static class UnaryOperatorExtensions
{
    public static string ToHumanString(this UnaryOperator op)
        => op switch
        {
            UnaryOperator.Negate => "-",
            UnaryOperator.LogicalNot => "NOT",
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

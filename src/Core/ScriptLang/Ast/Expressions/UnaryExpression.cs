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
    public UnaryOperator Operator { get; set; }
    public IExpression SubExpression { get; set; }

    public UnaryExpression(Token operatorToken, IExpression subExpression) : base(operatorToken)
    {
        Debug.Assert(operatorToken.Kind is TokenKind.NOT or TokenKind.Minus);
        Operator = UnaryOperatorExtensions.FromToken(operatorToken.Kind);
        SubExpression = subExpression;
    }
    public UnaryExpression(SourceRange source, UnaryOperator op, IExpression subExpression) : base(source)
        => (Operator, SubExpression) = (op, subExpression);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

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

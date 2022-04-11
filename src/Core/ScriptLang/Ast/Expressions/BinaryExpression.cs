namespace ScTools.ScriptLang.Ast.Expressions;

using System;

public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    And,
    Xor,
    Or,
    Equals,
    NotEquals,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LogicalAnd,
    LogicalOr,
}

public sealed class BinaryExpression : BaseExpression
{
    public BinaryOperator Operator { get; set; }
    public IExpression LHS { get; set; }
    public IExpression RHS { get; set; }
    /// <summary>
    /// Gets or sets the label used to implement the short-circuiting logic of <see cref="BinaryOperator.LogicalAnd"/> and <see cref="BinaryOperator.LogicalOr"/>.
    /// </summary>
    public string? ShortCircuitLabel { get; set; }

    public BinaryExpression(SourceRange source, BinaryOperator op, IExpression lhs, IExpression rhs) : base(source)
        => (Operator, LHS, RHS) = (op, lhs, rhs);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

public static class BinaryOperatorExtensions
{
    public static string ToToken(this BinaryOperator op)
        => op switch
        {
            BinaryOperator.Add => "+",
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => "*",
            BinaryOperator.Divide => "/",
            BinaryOperator.Modulo => "%",
            BinaryOperator.And => "&",
            BinaryOperator.Xor => "^",
            BinaryOperator.Or => "|",
            BinaryOperator.Equals => "==",
            BinaryOperator.NotEquals => "<>",
            BinaryOperator.LessThan => "<",
            BinaryOperator.LessThanOrEqual => "<=",
            BinaryOperator.GreaterThan => ">",
            BinaryOperator.GreaterThanOrEqual => ">=",
            BinaryOperator.LogicalAnd => "AND",
            BinaryOperator.LogicalOr => "OR",
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        };

    public static BinaryOperator FromToken(string token)
        => token.ToUpperInvariant() switch
        {
            "+" => BinaryOperator.Add,
            "-" => BinaryOperator.Subtract,
            "*" => BinaryOperator.Multiply,
            "/" => BinaryOperator.Divide,
            "%" => BinaryOperator.Modulo,
            "&" => BinaryOperator.And,
            "^" => BinaryOperator.Xor,
            "|" => BinaryOperator.Or,
            "==" => BinaryOperator.Equals,
            "<>" => BinaryOperator.NotEquals,
            "<" => BinaryOperator.LessThan,
            "<=" => BinaryOperator.LessThanOrEqual,
            ">" => BinaryOperator.GreaterThan,
            ">=" => BinaryOperator.GreaterThanOrEqual,
            "AND" => BinaryOperator.LogicalAnd,
            "OR" => BinaryOperator.LogicalOr,
            _ => throw new ArgumentException($"Unknown binary operator '{token}'", nameof(token)),
        };
}

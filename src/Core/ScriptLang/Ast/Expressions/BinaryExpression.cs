namespace ScTools.ScriptLang.Ast.Expressions;

using System;
using System.Diagnostics;

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
    public BinaryOperator Operator { get; }
    public IExpression LHS => (IExpression)Children[0];
    public IExpression RHS => (IExpression)Children[1];
    /// <summary>
    /// Gets or sets the label used to implement the short-circuiting logic of <see cref="BinaryOperator.LogicalAnd"/> and <see cref="BinaryOperator.LogicalOr"/>.
    /// </summary>
    public string? ShortCircuitLabel { get; set; }

    public BinaryExpression(Token operatorToken, IExpression lhs, IExpression rhs)
        : base(OfTokens(operatorToken), OfChildren(lhs, rhs))
    {
        Debug.Assert(operatorToken.Kind is
            TokenKind.Plus or TokenKind.Minus or TokenKind.Asterisk or TokenKind.Slash or
            TokenKind.Percent or TokenKind.Ampersand or TokenKind.Caret or TokenKind.Bar or
            TokenKind.EqualsEquals or TokenKind.LessThanGreaterThan or TokenKind.LessThan or
            TokenKind.LessThanEquals or TokenKind.GreaterThan or TokenKind.GreaterThanEquals or
            TokenKind.AND or TokenKind.OR);
        Operator = BinaryOperatorExtensions.FromToken(operatorToken.Kind);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(BinaryExpression)} {{ {nameof(Operator)} = {Operator}, {nameof(LHS)} = {LHS.DebuggerDisplay}, {nameof(RHS)} = {RHS.DebuggerDisplay} }}";
}

public static class BinaryOperatorExtensions
{
    public static string ToHumanString(this BinaryOperator op)
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

    public static BinaryOperator FromToken(TokenKind token)
        => token switch
        {
            TokenKind.Plus => BinaryOperator.Add,
            TokenKind.Minus => BinaryOperator.Subtract,
            TokenKind.Asterisk => BinaryOperator.Multiply,
            TokenKind.Slash => BinaryOperator.Divide,
            TokenKind.Percent => BinaryOperator.Modulo,
            TokenKind.Ampersand => BinaryOperator.And,
            TokenKind.Caret => BinaryOperator.Xor,
            TokenKind.Bar => BinaryOperator.Or,
            TokenKind.EqualsEquals => BinaryOperator.Equals,
            TokenKind.LessThanGreaterThan => BinaryOperator.NotEquals,
            TokenKind.LessThan => BinaryOperator.LessThan,
            TokenKind.LessThanEquals => BinaryOperator.LessThanOrEqual,
            TokenKind.GreaterThan => BinaryOperator.GreaterThan,
            TokenKind.GreaterThanEquals => BinaryOperator.GreaterThanOrEqual,
            TokenKind.AND => BinaryOperator.LogicalAnd,
            TokenKind.OR => BinaryOperator.LogicalOr,
            _ => throw new ArgumentException($"Unknown binary operator '{token}'", nameof(token)),
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

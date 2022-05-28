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

public sealed partial class BinaryExpression : BaseExpression
{
    public BinaryOperator Operator { get; }
    public IExpression LHS => (IExpression)Children[0];
    public IExpression RHS => (IExpression)Children[1];

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

    public override string DebuggerDisplay =>
        $@"{nameof(BinaryExpression)} {{ {nameof(Operator)} = {Operator}, {nameof(LHS)} = {LHS.DebuggerDisplay}, {nameof(RHS)} = {RHS.DebuggerDisplay} }}";
}

public static class BinaryOperatorExtensions
{
    public static TokenKind ToToken(this BinaryOperator op)
        => op switch
        {
            BinaryOperator.Add => TokenKind.Plus,
            BinaryOperator.Subtract => TokenKind.Minus,
            BinaryOperator.Multiply => TokenKind.Asterisk,
            BinaryOperator.Divide => TokenKind.Slash,
            BinaryOperator.Modulo => TokenKind.Percent,
            BinaryOperator.And => TokenKind.Ampersand,
            BinaryOperator.Xor => TokenKind.Caret,
            BinaryOperator.Or => TokenKind.Bar,
            BinaryOperator.Equals => TokenKind.EqualsEquals,
            BinaryOperator.NotEquals => TokenKind.LessThanGreaterThan,
            BinaryOperator.LessThan => TokenKind.LessThan,
            BinaryOperator.LessThanOrEqual => TokenKind.LessThanEquals,
            BinaryOperator.GreaterThan => TokenKind.GreaterThan,
            BinaryOperator.GreaterThanOrEqual => TokenKind.GreaterThanEquals,
            BinaryOperator.LogicalAnd => TokenKind.AND,
            BinaryOperator.LogicalOr => TokenKind.OR,
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
}

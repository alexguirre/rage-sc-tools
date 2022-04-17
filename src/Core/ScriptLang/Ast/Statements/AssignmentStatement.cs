namespace ScTools.ScriptLang.Ast.Statements;

using ScTools.ScriptLang.Ast.Expressions;

using System;
using System.Diagnostics;

public sealed class AssignmentStatement : BaseStatement
{
    public BinaryOperator? CompoundOperator { get; }
    public bool IsCompound => CompoundOperator != null;
    public IExpression LHS => (IExpression)Children[0];
    public IExpression RHS => (IExpression)Children[1];

    public AssignmentStatement(Token operatorToken, IExpression lhs, IExpression rhs, Label? label)
        : base(OfTokens(operatorToken), OfChildren(lhs, rhs), label)
    {
        Debug.Assert(IsAssignmentOperator(operatorToken.Kind));

        CompoundOperator = GetCompoundOperator(operatorToken.Kind);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);

    public override string DebuggerDisplay =>
        $@"{nameof(AssignmentStatement)} {{{(IsCompound ? $"{nameof(CompoundOperator)} = {CompoundOperator}," : "")} {nameof(LHS)} = {LHS.DebuggerDisplay}, {nameof(RHS)} = {RHS.DebuggerDisplay} }}";

    public static bool IsAssignmentOperator(TokenKind token)
        => token is TokenKind.Equals or
                    TokenKind.AsteriskEquals or TokenKind.SlashEquals or TokenKind.PercentEquals or
                    TokenKind.PlusEquals or TokenKind.MinusEquals or
                    TokenKind.AmpersandEquals or TokenKind.CaretEquals or TokenKind.BarEquals;

    public static bool IsCompoundAssignmentOperator(TokenKind token)
        => IsAssignmentOperator(token) && token is not TokenKind.Equals;

    public static BinaryOperator? GetCompoundOperator(TokenKind token)
        => token switch
        {
            TokenKind.Equals => null,
            TokenKind.PlusEquals => BinaryOperator.Add,
            TokenKind.MinusEquals => BinaryOperator.Subtract,
            TokenKind.AsteriskEquals => BinaryOperator.Multiply,
            TokenKind.SlashEquals => BinaryOperator.Divide,
            TokenKind.PercentEquals => BinaryOperator.Modulo,
            TokenKind.AmpersandEquals => BinaryOperator.And,
            TokenKind.CaretEquals => BinaryOperator.Xor,
            TokenKind.BarEquals => BinaryOperator.Or,
            _ => throw new ArgumentException($"The token '{token}' is not an assignment operator", nameof(token)),
        };
}

namespace ScTools.ScriptLang.Ast.Expressions;

using System;
using System.Diagnostics;

public enum PostfixUnaryOperator
{
    Increment,
    Decrement
}

public sealed partial class PostfixUnaryExpression : BaseExpression
{
    public PostfixUnaryOperator Operator { get; }
    public IExpression SubExpression => (IExpression)Children[0];

    public PostfixUnaryExpression(Token operatorToken, IExpression subExpression)
        : base(OfTokens(operatorToken), OfChildren(subExpression))
    {
        Debug.Assert(operatorToken.Kind is TokenKind.PlusPlus or TokenKind.MinusMinus);
        Operator = PostfixUnaryOperatorExtensions.FromToken(operatorToken.Kind);
    }

    public override string DebuggerDisplay =>
        $@"{nameof(PostfixUnaryOperator)} {{ {nameof(Operator)} = {Operator}, {nameof(SubExpression)} = {SubExpression.DebuggerDisplay} }}";
}

public static class PostfixUnaryOperatorExtensions
{
    public static string ToLexeme(this PostfixUnaryOperator op)
        => ToToken(op).GetCanonicalLexeme();

    public static TokenKind ToToken(this PostfixUnaryOperator op)
        => op switch
        {
            PostfixUnaryOperator.Increment => TokenKind.PlusPlus,
            PostfixUnaryOperator.Decrement => TokenKind.MinusMinus,
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        };

    public static PostfixUnaryOperator FromToken(TokenKind token)
        => token switch
        {
            TokenKind.PlusPlus => PostfixUnaryOperator.Increment,
            TokenKind.MinusMinus => PostfixUnaryOperator.Decrement,
            _ => throw new ArgumentException($"Unknown postfix unary operator '{token}'", nameof(token)),
        };

    public static PostfixUnaryOperator FromToken(string token)
        => token.ToUpperInvariant() switch
        {
            "++" => PostfixUnaryOperator.Increment,
            "--" => PostfixUnaryOperator.Decrement,
            _ => throw new ArgumentException($"Unknown postfix unary operator '{token}'", nameof(token)),
        };
}

﻿#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;

    public abstract class Expression : Node
    {
        public Expression(SourceRange source) : base(source)
        {
        }
    }

    public sealed class UnaryExpression : Expression
    {
        public UnaryOperator Op { get; }
        public Expression Operand { get; }

        public override IEnumerable<Node> Children { get { yield return Operand; } }

        public UnaryExpression(UnaryOperator op, Expression operand, SourceRange source) : base(source)
            => (Op, Operand) = (op, operand);

        public override string ToString() => $"{OpToString(Op)}{Operand}";

        public static string OpToString(UnaryOperator op) => op switch
        {
            UnaryOperator.Not => "NOT ",
            UnaryOperator.Negate => "-",
            _ => throw new NotImplementedException()
        };
    }

    public enum UnaryOperator
    {
        Not,
        Negate,
    }

    public sealed class BinaryExpression : Expression
    {
        public BinaryOperator Op { get; }
        public Expression Left { get; }
        public Expression Right { get; }

        public override IEnumerable<Node> Children { get { yield return Left; yield return Right; } }

        public BinaryExpression(BinaryOperator op, Expression left, Expression right, SourceRange source) : base(source)
            => (Op, Left, Right) = (op, left, right);

        public override string ToString() => $"({Left} {OpToString(Op)} {Right})";

        public static string OpToString(BinaryOperator op) => op switch
        {
            BinaryOperator.Add => "+",
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => "*",
            BinaryOperator.Divide => "/",
            BinaryOperator.Modulo => "%",
            BinaryOperator.Or => "|",
            BinaryOperator.And => "&",
            BinaryOperator.Xor => "^",
            BinaryOperator.Equal => "==",
            BinaryOperator.NotEqual => "!=",
            BinaryOperator.Greater => ">",
            BinaryOperator.GreaterOrEqual => ">=",
            BinaryOperator.Less => "<",
            BinaryOperator.LessOrEqual => "<=",
            BinaryOperator.LogicalAnd => "AND",
            BinaryOperator.LogicalOr => "OR",
            _ => throw new NotImplementedException()
        };

        public static bool OpIsComparison(BinaryOperator op)
        {
            switch (op)
            {
                case BinaryOperator.Equal:
                case BinaryOperator.NotEqual:
                case BinaryOperator.Greater:
                case BinaryOperator.GreaterOrEqual:
                case BinaryOperator.Less:
                case BinaryOperator.LessOrEqual: return true;
                default: return false;
            }
        }
    }

    public enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Or,
        And,
        Xor,

        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,

        LogicalAnd,
        LogicalOr,
    }

    public sealed class AggregateExpression : Expression
    {
        public ImmutableArray<Expression> Expressions { get; }

        public override IEnumerable<Node> Children => Expressions;

        public AggregateExpression(IEnumerable<Expression> expressions, SourceRange source) : base(source)
            => Expressions = expressions.ToImmutableArray();

        public override string ToString() => $"<<{string.Join(", ", Expressions)}>>";
    }

    public sealed class IdentifierExpression : Expression
    {
        public string Identifier { get; }

        public IdentifierExpression(string identifier, SourceRange source) : base(source)
            => Identifier = identifier;

        public override string ToString() => $"{Identifier}";
    }

    public sealed class MemberAccessExpression : Expression
    {
        public Expression Expression { get; }
        public string Member { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; } }

        public MemberAccessExpression(Expression expression, string member, SourceRange source) : base(source)
            => (Expression, Member) = (expression, member);

        public override string ToString() => $"{Expression}.{Member}";
    }

    public sealed class ArrayAccessExpression : Expression
    {
        public Expression Expression { get; }
        public ArrayIndexer Indexer { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; yield return Indexer; } }

        public ArrayAccessExpression(Expression expression, ArrayIndexer indexer, SourceRange source) : base(source)
            => (Expression, Indexer) = (expression, indexer);

        public override string ToString() => $"{Expression}{Indexer}";
    }

    public sealed class InvocationExpression : Expression
    {
        public Expression Expression { get; }
        public ArgumentList ArgumentList { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; yield return ArgumentList; } }

        public InvocationExpression(Expression expression, ArgumentList argumentList, SourceRange source) : base(source)
            => (Expression, ArgumentList) = (expression, argumentList);

        public override string ToString() => $"{Expression}{ArgumentList}";
    }

    public sealed class LiteralExpression : Expression
    {
        public LiteralKind Kind { get; }
        public string ValueText { get; }

        public int IntValue { get { Debug.Assert(Kind == LiteralKind.Int); return int.Parse(ValueText); } }
        public float FloatValue { get { Debug.Assert(Kind == LiteralKind.Float); return float.Parse(ValueText); } }
        public string StringValue
        { 
            get 
            { 
                Debug.Assert(Kind == LiteralKind.String); 
                return ValueText[1..^1].Unescape(); // remove quotes at start and end of the string, then unescape it
            }
        }
        public bool BoolValue { get { Debug.Assert(Kind == LiteralKind.Bool); return ValueText.ToUpperInvariant() == "TRUE"; } }

        public LiteralExpression(LiteralKind kind, string valueText, SourceRange source) : base(source)
            => (Kind, ValueText) = (kind, valueText);

        public override string ToString() => $"{ValueText}";
    }

    public enum LiteralKind
    {
        Int,
        Float,
        String,
        Bool,
    }
}

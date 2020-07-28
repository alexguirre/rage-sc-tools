#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public abstract class Expression : Node
    {
        public Expression(SourceLocation location) : base(location)
        {
        }
    }

    public sealed class NotExpression : Expression
    {
        public Expression Inner { get; }

        public override IEnumerable<Node> Children { get { yield return Inner; } }

        public NotExpression(Expression inner, SourceLocation location) : base(location)
            => Inner = inner;
    }

    public sealed class BinaryExpression : Expression
    {
        public BinaryOperator Op { get; }
        public Expression Left { get; }
        public Expression Right { get; }

        public override IEnumerable<Node> Children { get { yield return Left; yield return Right; } }

        public BinaryExpression(BinaryOperator op, Expression left, Expression right, SourceLocation location) : base(location)
            => (Op, Left, Right) = (op, left, right);
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
        Xor
    }

    public sealed class AggregateExpression : Expression
    {
        public ImmutableArray<Expression> Expressions { get; }

        public override IEnumerable<Node> Children => Expressions;

        public AggregateExpression(IEnumerable<Expression> expressions, SourceLocation location) : base(location)
            => Expressions = expressions.ToImmutableArray();
    }

    public sealed class IdentifierExpression : Expression
    {
        public Identifier Identifier { get; }

        public override IEnumerable<Node> Children { get { yield return Identifier; } }

        public IdentifierExpression(Identifier identifier, SourceLocation location) : base(location)
            => Identifier = identifier;
    }

    public sealed class MemberAccessExpression : Expression
    {
        public Expression Expression { get; }
        public Identifier Member { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; yield return Member; } }

        public MemberAccessExpression(Expression expression, Identifier member, SourceLocation location) : base(location)
            => (Expression, Member) = (expression, member);
    }

    public sealed class ArrayAccessExpression : Expression
    {
        public Expression Expression { get; }
        public ArrayIndexer Indexer { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; yield return Indexer; } }

        public ArrayAccessExpression(Expression expression, ArrayIndexer indexer, SourceLocation location) : base(location)
            => (Expression, Indexer) = (expression, indexer);
    }

    public sealed class CallExpression : Expression
    {
        public ProcedureCall Call { get; }

        public override IEnumerable<Node> Children { get { yield return Call; } }

        public CallExpression(ProcedureCall call, SourceLocation location) : base(location)
            => Call = call;
    }

    public sealed class LiteralExpression : Expression
    {
        public LiteralKind Kind { get; }
        public string ValueText { get; }

        public LiteralExpression(LiteralKind kind, string valueText, SourceLocation location) : base(location)
            => (Kind, ValueText) = (kind, valueText);
    }

    public enum LiteralKind
    {
        Numeric,
        String,
        Bool,
    }
}

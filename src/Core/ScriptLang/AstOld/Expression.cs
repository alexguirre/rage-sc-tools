#nullable enable
namespace ScTools.ScriptLang.AstOld
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    public abstract class Expression : Node
    {
        public Expression(SourceRange source) : base(source)
        {
        }
    }

    public sealed class ErrorExpression : Expression
    {
        public string Text { get; }

        public ErrorExpression(string text, SourceRange source) : base(source)
            => Text = text;

        public override string ToString() => Text;

        public override void Accept(AstVisitor visitor) => visitor.VisitErrorExpression(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitErrorExpression(this);
    }

    public sealed class UnaryExpression : Expression
    {
        public UnaryOperator Op { get; }
        public Expression Operand { get; }

        public override IEnumerable<Node> Children { get { yield return Operand; } }

        public UnaryExpression(UnaryOperator op, Expression operand, SourceRange source) : base(source)
            => (Op, Operand) = (op, operand);

        public override string ToString() => $"{OpToString(Op)}{Operand}";

        public override void Accept(AstVisitor visitor) => visitor.VisitUnaryExpression(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitUnaryExpression(this);

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

        public override void Accept(AstVisitor visitor) => visitor.VisitBinaryExpression(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitBinaryExpression(this);

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
            BinaryOperator.NotEqual => "<>",
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

    public sealed class VectorExpression : Expression
    {
        public Expression X { get; }
        public Expression Y { get; }
        public Expression Z { get; }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return X;
                yield return Y;
                yield return Z;
            }
        }

        public VectorExpression(Expression x, Expression y, Expression z, SourceRange source) : base(source)
            => (X, Y, Z) = (x, y, z);

        public override string ToString() => $"<<{X}, {Y}, {Z}>>";

        public override void Accept(AstVisitor visitor) => visitor.VisitVectorExpression(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitVectorExpression(this);
    }

    public sealed class IdentifierExpression : Expression
    {
        public string Identifier { get; }

        public IdentifierExpression(string identifier, SourceRange source) : base(source)
            => Identifier = identifier;

        public override string ToString() => $"{Identifier}";

        public override void Accept(AstVisitor visitor) => visitor.VisitIdentifierExpression(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitIdentifierExpression(this);
    }

    public sealed class MemberAccessExpression : Expression
    {
        public Expression Expression { get; }
        public string Member { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; } }

        public MemberAccessExpression(Expression expression, string member, SourceRange source) : base(source)
            => (Expression, Member) = (expression, member);

        public override string ToString() => $"{Expression}.{Member}";

        public override void Accept(AstVisitor visitor) => visitor.VisitMemberAccessExpression(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitMemberAccessExpression(this);
    }

    public sealed class ArrayAccessExpression : Expression
    {
        public Expression Expression { get; }
        public Expression Index { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; yield return Index; } }

        public ArrayAccessExpression(Expression expression, Expression index, SourceRange source) : base(source)
            => (Expression, Index) = (expression, index);

        public override string ToString() => $"{Expression}[{Index}]";

        public override void Accept(AstVisitor visitor) => visitor.VisitArrayAccessExpression(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitArrayAccessExpression(this);
    }

    public sealed class InvocationExpression : Expression
    {
        public Expression Expression { get; }
        public ImmutableArray<Expression> Arguments { get; }

        public override IEnumerable<Node> Children => Arguments.Prepend(Expression);

        public InvocationExpression(Expression expression, IEnumerable<Expression> arguments, SourceRange source) : base(source)
            => (Expression, Arguments) = (expression, arguments.ToImmutableArray());

        public override string ToString() => $"{Expression}({string.Join(", ", Arguments)})";

        public override void Accept(AstVisitor visitor) => visitor.VisitInvocationExpression(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitInvocationExpression(this);
    }

    public sealed class LiteralExpression : Expression
    {
        public LiteralKind Kind { get; }
        public string ValueText { get; }

        public int IntValue { get { Debug.Assert(Kind == LiteralKind.Int); return ValueText.ParseAsInt(); } }
        public float FloatValue { get { Debug.Assert(Kind == LiteralKind.Float); return ValueText.ParseAsFloat(); } }
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

        public override void Accept(AstVisitor visitor) => visitor.VisitLiteralExpression(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitLiteralExpression(this);
    }

    public enum LiteralKind
    {
        Int,
        Float,
        String,
        Bool,
    }
}

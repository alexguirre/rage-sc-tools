#nullable enable
namespace ScTools.ScriptLang.AstOld
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    public abstract class Statement : Node
    {
        public Statement(SourceRange source) : base(source)
        {
        }
    }

    public sealed class StatementBlock : Node
    {
        public ImmutableArray<Statement> Statements { get; }

        public override IEnumerable<Node> Children => Statements;

        public StatementBlock(IEnumerable<Statement> statements, SourceRange source) : base(source)
            => Statements = statements.ToImmutableArray();

        public override string ToString() => $"{string.Join("\n", Statements)}";

        public override void Accept(AstVisitor visitor) => visitor.VisitStatementBlock(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitStatementBlock(this);
    }

    public sealed class ErrorStatement : Statement
    {
        public string Text { get; }

        public ErrorStatement(string text, SourceRange source) : base(source)
            => Text = text;

        public override string ToString() => Text;

        public override void Accept(AstVisitor visitor) => visitor.VisitErrorStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitErrorStatement(this);
    }

    public sealed class VariableDeclarationStatement : Statement
    {
        public Declaration Declaration { get; }

        public override IEnumerable<Node> Children { get { yield return Declaration; } }

        public VariableDeclarationStatement(Declaration declaration, SourceRange source) : base(source)
            => Declaration = declaration;

        public override string ToString() => $"{Declaration}";

        public override void Accept(AstVisitor visitor) => visitor.VisitVariableDeclarationStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitVariableDeclarationStatement(this);
    }

    public sealed class AssignmentStatement : Statement
    {
        /// <summary>
        /// Operator used in case of compound assignment; otherwise, <c>null</c>.
        /// </summary>
        public BinaryOperator? Op { get; }
        public Expression Left { get; }
        public Expression Right { get; }

        public override IEnumerable<Node> Children { get { yield return Left; yield return Right; } }

        public AssignmentStatement(BinaryOperator? op, Expression left, Expression right, SourceRange source) : base(source)
            => (Op, Left, Right) = (op, left, right);

        public override string ToString() => $"{Left} {(Op == null ? "" : BinaryExpression.OpToString(Op.Value))}= {Right}";

        public override void Accept(AstVisitor visitor) => visitor.VisitAssignmentStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitAssignmentStatement(this);
    }

    public sealed class IfStatement : Statement
    {
        public Expression Condition { get; }
        public StatementBlock ThenBlock { get; }
        public StatementBlock? ElseBlock { get; }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Condition;
                yield return ThenBlock;
                if (ElseBlock != null)
                {
                    yield return ElseBlock;
                }
            }
        }

        public IfStatement(Expression condition, StatementBlock thenBlock, StatementBlock? elseBlock, SourceRange source) : base(source)
            => (Condition, ThenBlock, ElseBlock) = (condition, thenBlock, elseBlock);

        public override string ToString() => $"IF {Condition}\n{ThenBlock}\n{(ElseBlock != null ? $"ELSE\n{ElseBlock}\n" : "")}ENDIF";

        public override void Accept(AstVisitor visitor) => visitor.VisitIfStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitIfStatement(this);
    }

    public sealed class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return Condition; yield return Block; } }

        public WhileStatement(Expression condition, StatementBlock block, SourceRange source) : base(source)
            => (Condition, Block) = (condition, block);

        public override string ToString() => $"WHILE {Condition}\n{Block}\nENDWHILE";

        public override void Accept(AstVisitor visitor) => visitor.VisitWhileStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitWhileStatement(this);
    }

    public sealed class RepeatStatement : Statement
    {
        public Expression Limit { get; }
        public Expression Counter { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return Limit; yield return Counter; yield return Block; } }

        public RepeatStatement(Expression limit, Expression counter, StatementBlock block, SourceRange source) : base(source)
            => (Limit, Counter, Block) = (limit, counter, block);

        public override string ToString() => $"REPEAT {Limit} {Counter}\n{Block}\nENDREPEAT";

        public override void Accept(AstVisitor visitor) => visitor.VisitRepeatStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitRepeatStatement(this);
    }

    public sealed class SwitchStatement : Statement
    {
        public Expression Expression { get; }
        public ImmutableArray<SwitchCase> Cases { get; }

        public override IEnumerable<Node> Children => Cases.Cast<Node>().Prepend(Expression);

        public SwitchStatement(Expression expression, IEnumerable<SwitchCase> cases, SourceRange source) : base(source)
            => (Expression, Cases) = (expression, cases.ToImmutableArray());

        public override string ToString() => $"SWITCH {Expression}\n{string.Join("\n", Cases)}\nENDSWITCH";

        public override void Accept(AstVisitor visitor) => visitor.VisitSwitchStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitSwitchStatement(this);
    }

    public abstract class SwitchCase : Node
    {
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return Block; } }

        public SwitchCase(StatementBlock block, SourceRange source) : base(source)
            => Block = block;
    }

    public sealed class ValueSwitchCase : SwitchCase
    {
        public Expression Value { get; }

        public override IEnumerable<Node> Children { get { yield return Value; yield return Block; } }

        public ValueSwitchCase(Expression value, StatementBlock block, SourceRange source) : base(block, source)
            => Value = value;

        public override string ToString() => $"CASE {Value}\n{Block}";

        public override void Accept(AstVisitor visitor) => visitor.VisitValueSwitchCase(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitValueSwitchCase(this);
    }

    public sealed class DefaultSwitchCase : SwitchCase
    {
        public DefaultSwitchCase(StatementBlock block, SourceRange source) : base(block, source)
        { }

        public override string ToString() => $"DEFAULT\n{Block}";

        public override void Accept(AstVisitor visitor) => visitor.VisitDefaultSwitchCase(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitDefaultSwitchCase(this);
    }

    public sealed class ReturnStatement : Statement
    {
        public Expression? Expression { get; }

        public override IEnumerable<Node> Children { get { if (Expression != null) { yield return Expression; } } }

        public ReturnStatement(Expression? expression, SourceRange source) : base(source)
            => Expression = expression;

        public override string ToString() => $"RETURN{(Expression != null ? $" {Expression}": "")}";

        public override void Accept(AstVisitor visitor) => visitor.VisitReturnStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitReturnStatement(this);
    }

    public sealed class InvocationStatement : Statement
    {
        public Expression Expression { get; }
        public ImmutableArray<Expression> Arguments { get; }

        public override IEnumerable<Node> Children => Arguments.Prepend(Expression);

        public InvocationStatement(Expression expression, IEnumerable<Expression> arguments, SourceRange source) : base(source)
            => (Expression, Arguments) = (expression, arguments.ToImmutableArray());

        public override string ToString() => $"{Expression}({string.Join(", ", Arguments)})";

        public override void Accept(AstVisitor visitor) => visitor.VisitInvocationStatement(this);
        [return: MaybeNull] public override T Accept<T>(AstVisitor<T> visitor) => visitor.VisitInvocationStatement(this);
    }
}

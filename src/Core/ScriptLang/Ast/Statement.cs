#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

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
    }

    public sealed class VariableDeclarationStatement : Statement
    {
        public Variable Variable { get; }
        public Expression? Initializer { get; }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Variable;
                if (Initializer != null)
                {
                    yield return Initializer;
                }
            }
        }

        public VariableDeclarationStatement(Variable variable, Expression? initializer, SourceRange source) : base(source)
            => (Variable, Initializer) = (variable, initializer);

        public override string ToString() => $"{Variable}" + (Initializer != null ? $" = {Initializer}" : "");
    }

    public sealed class AssignmentStatement : Statement
    {
        public Expression Left { get; }
        public Expression Right { get; }

        public override IEnumerable<Node> Children { get { yield return Left; yield return Right; } }

        public AssignmentStatement(Expression left, Expression right, SourceRange source) : base(source)
            => (Left, Right) = (left, right);

        public override string ToString() => $"{Left} = {Right}";
    }

    public sealed class IfStatement : Statement
    {
        public Expression Condition { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return Condition; yield return Block; } }

        public IfStatement(Expression condition, StatementBlock block, SourceRange source) : base(source)
            => (Condition, Block) = (condition, block);

        public override string ToString() => $"IF {Condition}\n{Block}\nENDIF";
    }

    public sealed class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return Condition; yield return Block; } }

        public WhileStatement(Expression condition, StatementBlock block, SourceRange source) : base(source)
            => (Condition, Block) = (condition, block);

        public override string ToString() => $"WHILE {Condition}\n{Block}\nENDWHILE";
    }

    public sealed class CallStatement : Statement
    {
        public ProcedureCall Call { get; }

        public override IEnumerable<Node> Children { get { yield return Call; } }

        public CallStatement(ProcedureCall call, SourceRange source) : base(source)
            => Call = call;

        public override string ToString() => $"{Call}";
    }
}

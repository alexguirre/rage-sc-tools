#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public abstract class Statement : Node
    {
        public Statement(SourceLocation location) : base(location)
        {
        }
    }

    public sealed class StatementBlock : Node
    {
        public ImmutableArray<Statement> Statements { get; }

        public override IEnumerable<Node> Children => Statements;

        public StatementBlock(IEnumerable<Statement> statements, SourceLocation location) : base(location)
            => Statements = statements.ToImmutableArray();
    }

    // TODO: VariableDeclarationStatement
    //public sealed class VariableDeclarationStatement : Statement
    //{

    //}

    public sealed class AssignmentStatement : Statement
    {
        public Expression Left { get; }
        public Expression Right { get; }

        public override IEnumerable<Node> Children { get { yield return Left; yield return Right; } }

        public AssignmentStatement(Expression left, Expression right, SourceLocation location) : base(location)
            => (Left, Right) = (left, right);
    }

    public sealed class IfStatement : Statement
    {
        public Expression Condition { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return Condition; yield return Block; } }

        public IfStatement(Expression condition, StatementBlock block, SourceLocation location) : base(location)
            => (Condition, Block) = (condition, block);
    }

    public sealed class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public StatementBlock Block { get; }

        public override IEnumerable<Node> Children { get { yield return Condition; yield return Block; } }

        public WhileStatement(Expression condition, StatementBlock block, SourceLocation location) : base(location)
            => (Condition, Block) = (condition, block);
    }

    public sealed class CallStatement : Statement
    {
        public ProcedureCall Call { get; }

        public override IEnumerable<Node> Children { get { yield return Call; } }

        public CallStatement(ProcedureCall call, SourceLocation location) : base(location)
            => Call = call;
    }
}

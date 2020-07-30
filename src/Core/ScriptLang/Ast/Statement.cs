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

    public sealed class ReturnStatement : Statement
    {
        public Expression Expression { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; } }

        public ReturnStatement(Expression expression, SourceRange source) : base(source)
            => Expression = expression;

        public override string ToString() => $"RETURN {Expression}";
    }

    public sealed class InvocationStatement : Statement
    {
        public Expression Expression { get; }
        public ArgumentList ArgumentList { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; yield return ArgumentList; } }

        public InvocationStatement(Expression expression, ArgumentList argumentList, SourceRange source) : base(source)
            => (Expression, ArgumentList) = (expression, argumentList);

        public override string ToString() => $"{Expression}{ArgumentList}";
    }
}

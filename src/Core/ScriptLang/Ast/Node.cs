#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public readonly struct SourceLocation
    {
        public static readonly SourceLocation Unknown = default;

        public bool IsUnknown => Line == 0 || Column == 0;
        public int Line { get; }
        public int Column { get; }

        public SourceLocation(int line, int column) => (Line, Column) = (line, column);
    }

    public abstract class Node
    {
        public SourceLocation Location { get; }
        public virtual IEnumerable<Node> Children => Enumerable.Empty<Node>();

        public Node(SourceLocation location) => Location = location;
    }

    public sealed class ArrayIndexer : Node
    {
        public Expression Expression { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; } }

        public ArrayIndexer(Expression expression, SourceLocation location) : base(location)
            => Expression = expression;
    }

    public sealed class ProcedureCall : Node
    {
        public Identifier Procedure { get; }
        public ImmutableArray<Expression> Arguments { get; }

        public override IEnumerable<Node> Children => Arguments.Cast<Node>().Prepend(Procedure);

        public ProcedureCall(Identifier procedure, IEnumerable<Expression> arguments, SourceLocation location) : base(location)
            => (Procedure, Arguments) = (procedure, arguments.ToImmutableArray());
    }
}

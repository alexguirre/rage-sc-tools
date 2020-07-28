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

    public sealed class Identifier : Node
    {
        public string Name { get; }

        public Identifier(string name, SourceLocation location) : base(location)
            => Name = name;

        public override string ToString() => $"{Name}";
    }

    public sealed class ArrayIndexer : Node
    {
        public Expression Expression { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; } }

        public ArrayIndexer(Expression expression, SourceLocation location) : base(location)
            => Expression = expression;

        public override string ToString() => $"[{Expression}]";
    }

    public sealed class Type : Node
    {
        public Identifier Name { get; }

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public Type(Identifier name, SourceLocation location) : base(location)
            => Name = name;

        public override string ToString() => $"{Name}";
    }

    public sealed class Variable : Node
    {
        public Type Type { get; }
        public Identifier Name { get; }
        public ArrayIndexer? ArrayRank { get; }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Type;
                yield return Name;
                if (ArrayRank != null)
                {
                    yield return ArrayRank;
                }
            }
        }

        public Variable(Type type, Identifier name, ArrayIndexer? arrayRank, SourceLocation location) : base(location)
            => (Type, Name, ArrayRank) = (type, name, arrayRank);

        public override string ToString() => $"{Type} {Name}{ArrayRank?.ToString() ?? ""}";
    }

    public sealed class ProcedureCall : Node
    {
        public Identifier Procedure { get; }
        public ImmutableArray<Expression> Arguments { get; }

        public override IEnumerable<Node> Children => Arguments.Cast<Node>().Prepend(Procedure);

        public ProcedureCall(Identifier procedure, IEnumerable<Expression> arguments, SourceLocation location) : base(location)
            => (Procedure, Arguments) = (procedure, arguments.ToImmutableArray());

        public override string ToString() => $"{Procedure}({string.Join(", ", Arguments)})";
    }
}

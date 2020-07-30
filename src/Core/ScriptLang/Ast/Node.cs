#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public abstract class Node
    {
        public SourceRange Source { get; }
        public virtual IEnumerable<Node> Children => Enumerable.Empty<Node>();

        public Node(SourceRange source) => Source = source;
    }

    public sealed class Identifier : Node
    {
        public string Name { get; }

        public Identifier(string name, SourceRange source) : base(source)
            => Name = name;

        public override string ToString() => $"{Name}";
    }

    public sealed class ArrayIndexer : Node
    {
        public Expression Expression { get; }

        public override IEnumerable<Node> Children { get { yield return Expression; } }

        public ArrayIndexer(Expression expression, SourceRange source) : base(source)
            => Expression = expression;

        public override string ToString() => $"[{Expression}]";
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

        public Variable(Type type, Identifier name, ArrayIndexer? arrayRank, SourceRange source) : base(source)
            => (Type, Name, ArrayRank) = (type, name, arrayRank);

        public override string ToString() => $"{Type} {Name}{ArrayRank?.ToString() ?? ""}";
    }

    public sealed class ParameterList : Node
    {
        public ImmutableArray<Variable> Parameters { get; }

        public override IEnumerable<Node> Children => Parameters;

        public ParameterList(IEnumerable<Variable> parameters, SourceRange source) : base(source)
            => Parameters = parameters.ToImmutableArray();

        public override string ToString() => $"({string.Join(", ", Parameters)})";
    }

    public sealed class ArgumentList : Node
    {
        public ImmutableArray<Expression> Arguments { get; }

        public override IEnumerable<Node> Children => Arguments;

        public ArgumentList(IEnumerable<Expression> arguments, SourceRange source) : base(source)
            => Arguments = arguments.ToImmutableArray();

        public override string ToString() => $"({string.Join(", ", Arguments)})";
    }
}

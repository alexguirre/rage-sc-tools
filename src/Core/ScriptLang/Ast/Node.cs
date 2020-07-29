#nullable enable
namespace ScTools.ScriptLang.Ast
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;

    public readonly struct SourceLocation : IEquatable<SourceLocation>
    {
        public static readonly SourceLocation Unknown = default;

        public bool IsUnknown => Line == default || Column == default;
        public int Line { get; }
        public int Column { get; }

        public SourceLocation(int line, int column) => (Line, Column) = (line, column);

        public override string ToString() => $"{{ {nameof(Line)}: {Line}, {nameof(Column)}: {Column} }}";

        public void Deconstruct(out int line, out int column) => (line, column) = (Line, Column);

        public bool Equals(SourceLocation other) => (Line, Column).Equals((other.Line, other.Column));
        public override int GetHashCode() => (Line, Column).GetHashCode();
        public override bool Equals(object? obj) => obj is SourceLocation l && Equals(l);

        public static implicit operator SourceLocation((int Line, int Column) location) => new SourceLocation(location.Line, location.Column);
    }

    public readonly struct SourceRange : IEquatable<SourceRange>
    {
        public static readonly SourceRange Unknown = default;

        public bool IsUnknown => Start.IsUnknown || End.IsUnknown;
        public SourceLocation Start { get; }
        public SourceLocation End { get; }

        public SourceRange(SourceLocation start, SourceLocation end) => (Start, End) = (start, end);

        public override string ToString()
            => $"{{ {nameof(Start)}: {Start}, {nameof(End)}: {End} }}";

        public void Deconstruct(out SourceLocation start, out SourceLocation end) => (start, end) = (Start, End);

        public bool Equals(SourceRange other) => (Start, End).Equals((other.Start, other.End));
        public override int GetHashCode() => (Start, End).GetHashCode();
        public override bool Equals(object? obj) => obj is SourceLocation l && Equals(l);

        public static SourceRange FromTokens(IToken start, IToken stop)
            => new SourceRange((start.Line, start.Column + 1), (stop.Line, stop.Column + 1 + Interval.Of(stop.StartIndex, stop.StopIndex).Length));
    }

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

    public sealed class Type : Node
    {
        public Identifier Name { get; }

        public override IEnumerable<Node> Children { get { yield return Name; } }

        public Type(Identifier name, SourceRange source) : base(source)
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

    public sealed class ProcedureCall : Node
    {
        public Identifier Procedure { get; }
        public ImmutableArray<Expression> Arguments { get; }

        public override IEnumerable<Node> Children => Arguments.Cast<Node>().Prepend(Procedure);

        public ProcedureCall(Identifier procedure, IEnumerable<Expression> arguments, SourceRange source) : base(source)
            => (Procedure, Arguments) = (procedure, arguments.ToImmutableArray());

        public override string ToString() => $"{Procedure}({string.Join(", ", Arguments)})";
    }
}

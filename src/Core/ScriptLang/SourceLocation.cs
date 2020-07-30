#nullable enable
namespace ScTools.ScriptLang
{
    using System;

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
}

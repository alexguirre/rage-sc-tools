namespace ScTools;

using System;
using System.Diagnostics;

/// <summary>
/// Represents a position within a source file as line and column, 1-based.
/// </summary>
public readonly struct SourceLocation : IEquatable<SourceLocation>, IComparable<SourceLocation>
{
    public static readonly SourceLocation Unknown = default;
    public static SourceLocation EOF(string filePath) => new(0, 0, filePath);

    public bool IsUnknown => Line == 0 && Column == 0 && FilePath == null;
    public bool IsEOF => Line == 0 && Column == 0 && FilePath != null;
    public int Line { get; }
    public int Column { get; }
    public string FilePath { get; }

    public SourceLocation(int line, int column, string filePath) => (Line, Column, FilePath) = (line, column, filePath);

    public override string ToString() => $"{{ {nameof(Line)}: {Line}, {nameof(Column)}: {Column}, {nameof(FilePath)}: '{FilePath}' }}";

    public void Deconstruct(out int line, out int column, out string filePath) => (line, column, filePath) = (Line, Column, FilePath);

    public bool Equals(SourceLocation other) => (Line, Column).Equals((other.Line, other.Column));

    public int CompareTo(SourceLocation other)
    {
        if (Line == other.Line)
        {
            return Column.CompareTo(other.Column);
        }
        else
        {
            return Line.CompareTo(other.Line);
        }
    }

    public override int GetHashCode() => (Line, Column).GetHashCode();
    public override bool Equals(object? obj) => obj is SourceLocation l && Equals(l);

    public static implicit operator SourceLocation((int Line, int Column, string FilePath) location) => new(location.Line, location.Column, location.FilePath);

    public static bool operator <(SourceLocation left, SourceLocation right) => left.CompareTo(right) < 0;
    public static bool operator <=(SourceLocation left, SourceLocation right) => left.CompareTo(right) <= 0;
    public static bool operator >(SourceLocation left, SourceLocation right) => left.CompareTo(right) > 0;
    public static bool operator >=(SourceLocation left, SourceLocation right) => left.CompareTo(right) >= 0;

    public static bool operator ==(SourceLocation left, SourceLocation right) => left.Equals(right);
    public static bool operator !=(SourceLocation left, SourceLocation right) => !left.Equals(right);
}

/// <summary>
/// Represents a range [start..end] within a source file.
/// </summary>
public readonly struct SourceRange : IEquatable<SourceRange>
{
    public static readonly SourceRange Unknown = default;
    public static SourceRange EOF(string filePath) => new((0, 0, filePath), (0, 0, filePath));

    public bool IsUnknown => Start.IsUnknown || End.IsUnknown;
    public bool IsEOF => Start.IsEOF || End.IsEOF;
    public string FilePath => Start.FilePath;
    public SourceLocation Start { get; }
    public SourceLocation End { get; } // inclusive

    public SourceRange(SourceLocation start, SourceLocation end)
    {
        Debug.Assert(start.FilePath == end.FilePath);
        (Start, End) = (start, end);
    }

    public override string ToString()
        => $"{{ {nameof(Start)}: {Start}, {nameof(End)}: {End} }}";

    public void Deconstruct(out SourceLocation start, out SourceLocation end)
        => (start, end) = (Start, End);

    public bool Contains(SourceLocation location) => location >= Start && location <= End;
    public bool Contains(SourceRange range) => FilePath == range.FilePath && Contains(range.Start) && Contains(range.End);
    public SourceRange Merge(SourceRange other)
    {
        Debug.Assert(FilePath == other.FilePath);
        var start = Start < other.Start ? Start : other.Start;
        var end = End > other.End ? End : other.End;
        return new SourceRange(start, end);
    }
    public bool Equals(SourceRange other) => (Start, End).Equals((other.Start, other.End));

    public override int GetHashCode() => (Start, End).GetHashCode();
    public override bool Equals(object? obj) => obj is SourceLocation l && Equals(l);

    public static bool operator ==(SourceRange left, SourceRange right) => left.Equals(right);
    public static bool operator !=(SourceRange left, SourceRange right) => !left.Equals(right);
}

namespace ScTools;

using System.Collections.Generic;
using System.IO;
using System.Linq;

public enum DiagnosticTag
{
    Error,
    Warning,
}

public sealed class Diagnostic
{
    public int Code { get; } = -1;
    public DiagnosticTag Tag { get; }
    public string Message { get; }
    public SourceRange Source { get; }

    public Diagnostic(int code, DiagnosticTag tag, string message, SourceRange source)
        => (Code, Tag, Message, Source) = (code, tag, message, source);

    public override string ToString()
        => $"{Source.FilePath}:{Source.Start.Line}:{Source.Start.Column}: {char.ToUpperInvariant(Tag.ToString().First())}{Code:X4}: {Message}";
}

public sealed class DiagnosticsReport
{
    private readonly List<Diagnostic> diagnostics = new();

    public bool HasErrors => diagnostics.Any(d => d.Tag is DiagnosticTag.Error);
    public bool HasWarnings => diagnostics.Any(d => d.Tag is DiagnosticTag.Warning);

    public Diagnostic[] AllDiagnostics => diagnostics.ToArray();
    public Diagnostic[] Errors => diagnostics.Where(d => d.Tag is DiagnosticTag.Error).ToArray();
    public Diagnostic[] Warnings => diagnostics.Where(d => d.Tag is DiagnosticTag.Warning).ToArray();

    public void Add(Diagnostic diagnostic) => diagnostics.Add(diagnostic);
    public Diagnostic Add(int code, DiagnosticTag tag, string message, SourceRange source)
    {
        var d = new Diagnostic(code, tag, message, source);
        Add(d);
        return d;
    }
    public void AddError(string message, SourceRange source) => Add(new Diagnostic(-1, DiagnosticTag.Error, message, source));
    public void AddWarning(string message, SourceRange source) => Add(new Diagnostic(-1, DiagnosticTag.Warning, message, source));

    public void Clear() => diagnostics.Clear();

    public void PrintAll(TextWriter dest)
    {
        foreach (var d in diagnostics.OrderBy(d => d.Source.FilePath).ThenBy(d => d.Source.Start))
        {
            dest.WriteLine(d);
        }
    }

    public static DiagnosticsReport Combine(IEnumerable<DiagnosticsReport> diagnosticsReports)
    {
        var result = new DiagnosticsReport();
        result.diagnostics.Capacity = diagnosticsReports.Sum(d => d.diagnostics.Count);
        foreach (var diagnosticsReport in diagnosticsReports)
        {
            result.diagnostics.AddRange(diagnosticsReport.diagnostics);
        }
        return result;
    }
}

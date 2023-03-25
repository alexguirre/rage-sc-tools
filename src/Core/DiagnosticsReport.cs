namespace ScTools;

using System.Collections.Generic;
using System.IO;
using System.Linq;

public enum DiagnosticSeverity
{
    Error,
    Warning,
}

public sealed class Diagnostic
{
    public int Code { get; }
    public DiagnosticSeverity Severity { get; }
    public string Message { get; }
    public SourceRange Source { get; }

    public Diagnostic(int code, DiagnosticSeverity severity, string message, SourceRange source)
        => (Code, Severity, Message, Source) = (code, severity, message, source);

    public override string ToString()
        => $"{Source.FilePath}:{Source.Start.Line}:{Source.Start.Column}: {char.ToUpperInvariant(Severity.ToString().First())}{Code:X4}: {Message}";
}

public sealed class DiagnosticsReport
{
    private readonly List<Diagnostic>[] diagnostics = { new(), new() }; // indexed by severity

    public bool HasErrors => Errors.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;

    public IEnumerable<Diagnostic> AllDiagnostics => diagnostics.SelectMany(d => d);
    public IReadOnlyList<Diagnostic> Errors => diagnostics[(int)DiagnosticSeverity.Error];
    public IReadOnlyList<Diagnostic> Warnings => diagnostics[(int)DiagnosticSeverity.Warning];

    public void Add(Diagnostic diagnostic) => diagnostics[(int)diagnostic.Severity].Add(diagnostic);
    public Diagnostic Add(int code, DiagnosticSeverity severity, string message, SourceRange source)
    {
        var d = new Diagnostic(code, severity, message, source);
        Add(d);
        return d;
    }
    public void AddError(string message, SourceRange source) => Add(new Diagnostic(-1, DiagnosticSeverity.Error, message, source));
    public void AddWarning(string message, SourceRange source) => Add(new Diagnostic(-1, DiagnosticSeverity.Warning, message, source));

    public void Clear() => diagnostics.ForEach(d => d.Clear());

    public IEnumerable<Diagnostic> GetDiagnosticsSorted()
        => AllDiagnostics.OrderBy(d => d.Source.FilePath).ThenBy(d => d.Source.Start);

    public static DiagnosticsReport Combine(IEnumerable<DiagnosticsReport> diagnosticsReports)
    {
        var src = diagnosticsReports.ToArray();
        var result = new DiagnosticsReport();
        for (int i = 0; i < result.diagnostics.Length; i++)
        {
            result.diagnostics[i].Capacity = src.Sum(d => d.diagnostics[i].Count);
            foreach (var srcReport in src)
            {
                result.diagnostics[i].AddRange(srcReport.diagnostics[i]);
            }
        }
        return result;
    }
}

#nullable enable
namespace ScTools
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public abstract class Diagnostic
    {
        public string Message { get; }
        public SourceRange Source { get; }

        public Diagnostic(string message, SourceRange source)
            => (Message, Source) = (message, source);

        public abstract void Print(TextWriter dest);
    }

    public sealed class ErrorDiagnostic : Diagnostic
    {
        public ErrorDiagnostic(string message, SourceRange source) : base(message, source) { }

        public override void Print(TextWriter dest) => dest.Write($"{Source.Start.Line}:{Source.Start.Column}: error: {Message}");
    }

    public sealed class WarningDiagnostic : Diagnostic
    {
        public WarningDiagnostic(string message, SourceRange source) : base(message, source) { }

        public override void Print(TextWriter dest) => dest.Write($"{Source.Start.Line}:{Source.Start.Column}: warning: {Message}");
    }

    public sealed class DiagnosticsReport
    {
        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();

        public string FilePath { get; }
        public bool HasErrors => diagnostics.Any(d => d is ErrorDiagnostic);
        public bool HasWarnings => diagnostics.Any(d => d is WarningDiagnostic);

        public IEnumerable<Diagnostic> AllDiagnostics => diagnostics;
        public IEnumerable<ErrorDiagnostic> Errors => diagnostics.OfType<ErrorDiagnostic>();
        public IEnumerable<WarningDiagnostic> Warnings => diagnostics.OfType<WarningDiagnostic>();

        public DiagnosticsReport(string filePath)
            => FilePath = filePath;

        public void Add(Diagnostic diagnostic) => diagnostics.Add(diagnostic);
        public void AddError(string message, SourceRange source) => Add(new ErrorDiagnostic(message, source));
        public void AddWarning(string message, SourceRange source) => Add(new WarningDiagnostic(message, source));

        public void Clear() => diagnostics.Clear();

        public void PrintAll(TextWriter dest)
        {
            foreach (var d in diagnostics)
            {
                dest.Write(FilePath);
                dest.Write(':');
                d.Print(dest);
                dest.WriteLine();
            }
        }
    }

    public sealed class Diagnostics
    {
        public readonly Dictionary<string, DiagnosticsReport> reportsByFile = new();

        public bool HasErrors => reportsByFile.Values.Any(r => r.HasErrors);
        public bool HasWarnings => reportsByFile.Values.Any(r => r.HasWarnings);

        public IEnumerable<Diagnostic> AllDiagnostics => reportsByFile.Values.SelectMany(r => r.AllDiagnostics);
        public IEnumerable<ErrorDiagnostic> Errors => reportsByFile.Values.SelectMany(r => r.Errors);
        public IEnumerable<WarningDiagnostic> Warnings => reportsByFile.Values.SelectMany(r => r.Warnings);

        public DiagnosticsReport this[string filePath] => GetReport(filePath);

        public Diagnostics()
        {
        }

        public Diagnostics(IEnumerable<DiagnosticsReport> reports)
        {
            foreach (var report in reports)
            {
                reportsByFile.Add(report.FilePath, report);
            }
        }

        public DiagnosticsReport GetReport(string filePath)
        {
            if (reportsByFile.TryGetValue(filePath, out var report))
            {
                return report;
            }

            report = new DiagnosticsReport(filePath);
            reportsByFile.Add(filePath, report);
            return report;
        }

        public void PrintAll(TextWriter dest)
        {
            foreach (var d in reportsByFile.Values)
            {
                d.PrintAll(dest);
            }
        }
    }
}

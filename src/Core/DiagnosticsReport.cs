namespace ScTools
{
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
        public DiagnosticTag Tag { get; }
        public string Message { get; }
        public SourceRange Source { get; }

        public Diagnostic(DiagnosticTag tag, string message, SourceRange source)
            => (Tag, Message, Source) = (tag, message, source);

        public override string ToString()
            => $"{Source.FilePath}:{Source.Start.Line}:{Source.Start.Column}: {Tag.ToString().ToLowerInvariant()}: {Message}";
    }

    public sealed class DiagnosticsReport
    {
        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();

        public bool HasErrors => diagnostics.Any(d => d.Tag is DiagnosticTag.Error);
        public bool HasWarnings => diagnostics.Any(d => d.Tag is DiagnosticTag.Warning);

        public IEnumerable<Diagnostic> AllDiagnostics => diagnostics;
        public IEnumerable<Diagnostic> Errors => diagnostics.Where(d => d.Tag is DiagnosticTag.Error);
        public IEnumerable<Diagnostic> Warnings => diagnostics.Where(d => d.Tag is DiagnosticTag.Warning);

        public void Add(Diagnostic diagnostic) => diagnostics.Add(diagnostic);
        public void AddError(string message, SourceRange source) => Add(new Diagnostic(DiagnosticTag.Error, message, source));
        public void AddWarning(string message, SourceRange source) => Add(new Diagnostic(DiagnosticTag.Warning, message, source));

        public void Clear() => diagnostics.Clear();

        public void PrintAll(TextWriter dest)
        {
            foreach (var d in diagnostics)
            {
                dest.WriteLine(d);
            }
        }
    }
}

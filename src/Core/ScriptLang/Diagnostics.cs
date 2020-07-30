#nullable enable
namespace ScTools.ScriptLang
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public abstract class Diagnostic
    {
        public string FilePath { get; }
        public string Message { get; }
        public SourceRange Source { get; }

        public Diagnostic(string filePath, string message, SourceRange source)
            => (FilePath, Message, Source) = (filePath, message, source);

        public abstract void Print(TextWriter dest);
    }

    public sealed class ErrorDiagnostic : Diagnostic
    {
        public ErrorDiagnostic(string filePath, string message, SourceRange source) : base(filePath, message, source) { }

        public override void Print(TextWriter dest) => dest.WriteLine($"{FilePath}:{Source.Start.Line}:{Source.Start.Column}: error: {Message}");
    }

    public sealed class WarningDiagnostic : Diagnostic
    {
        public WarningDiagnostic(string filePath, string message, SourceRange source) : base(filePath, message, source) { }

        public override void Print(TextWriter dest) => dest.WriteLine($"{FilePath}:{Source.Start.Line}:{Source.Start.Column}: warning: {Message}");
    }

    public sealed class Diagnostics
    {
        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();

        public bool HasErrors => diagnostics.Any(d => d is ErrorDiagnostic);
        public bool HasWarnings => diagnostics.Any(d => d is WarningDiagnostic);

        public IEnumerable<Diagnostic> AllDiagnostics => diagnostics;
        public IEnumerable<ErrorDiagnostic> Errors => diagnostics.Where(d => d is ErrorDiagnostic).Cast<ErrorDiagnostic>();
        public IEnumerable<WarningDiagnostic> Warnings => diagnostics.Where(d => d is WarningDiagnostic).Cast<WarningDiagnostic>();

        public void AddError(string filePath, string message, SourceRange source) => diagnostics.Add(new ErrorDiagnostic(filePath, message, source));
        public void AddWarning(string filePath, string message, SourceRange source) => diagnostics.Add(new WarningDiagnostic(filePath, message, source));
    }
}

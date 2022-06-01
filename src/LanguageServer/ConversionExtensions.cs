namespace ScTools.LanguageServer;

using System;

using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using LspPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using LspDiagnostic = Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic;
using LspDiagnosticSeverity = Microsoft.VisualStudio.LanguageServer.Protocol.DiagnosticSeverity;

internal static class ConversionExtensions
{
    public static LspRange ToLspRange(this SourceRange r)
        => r.IsUnknown ? new LspRange() : new LspRange
        {
            Start = r.Start.ToLspPosition(),
            End = r.End.ToLspPosition(),
        };

    public static LspPosition ToLspPosition(this SourceLocation l)
        => l.IsUnknown ? new LspPosition() : new LspPosition(l.Line - 1, l.Column - 1);

    public static LspDiagnostic ToLspDiagnostic(this Diagnostic diagnostic)
        => new()
        {
            Range = diagnostic.Source.ToLspRange(),
            Message = diagnostic.Message,
            Code = diagnostic.Code,
            Severity = diagnostic.Tag switch
            {
                DiagnosticTag.Error => LspDiagnosticSeverity.Error,
                DiagnosticTag.Warning => LspDiagnosticSeverity.Warning,
                _ => throw new InvalidOperationException("Unknown diagnostic tag"),
            },
        };
}

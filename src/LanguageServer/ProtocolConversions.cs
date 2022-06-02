namespace ScTools.LanguageServer;

using System;

using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using LspPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using LspDiagnostic = Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic;
using LspDiagnosticSeverity = Microsoft.VisualStudio.LanguageServer.Protocol.DiagnosticSeverity;

internal static class ProtocolConversions
{
    public static SourceRange FromLspRange(LspRange r, string filePath = "")
        => new(FromLspPosition(r.Start, filePath), FromLspPosition(r.End, filePath));

    public static SourceLocation FromLspPosition(LspPosition p, string filePath = "")
        => new(p.Line + 1, p.Character + 1, filePath);

    public static LspRange ToLspRange(SourceRange r)
        => r.IsUnknown ? new LspRange() : new LspRange
        {
            Start = ToLspPosition(r.Start),
            End = ToLspPosition(new(r.End.Line, r.End.Column + 1, r.FilePath)),
        };

    public static LspPosition ToLspPosition(SourceLocation l)
        => l.IsUnknown ? new LspPosition() : new LspPosition(l.Line - 1, l.Column - 1);

    public static LspDiagnostic ToLspDiagnostic(Diagnostic diagnostic)
        => new()
        {
            Range = ToLspRange(diagnostic.Source),
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

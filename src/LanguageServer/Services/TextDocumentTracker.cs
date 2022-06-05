namespace ScTools.LanguageServer.Services;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Semantics;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public interface ITextDocumentTracker
{
    Task<CompilationUnit?> GetDocumentAstAsync(Uri uri);
    Task OpenDocumentAsync(Uri uri, string text);
    Task UpdateDocumentAsync(Uri uri, string text);
    Task CloseDocumentAsync(Uri uri);
}

public class TextDocumentTracker : ITextDocumentTracker
{
    private IDiagnosticsPublisher diagnosticsPublisher;
    private readonly Dictionary<Uri, ScriptFile> files = new();

    public TextDocumentTracker(IDiagnosticsPublisher diagnosticsPublisher)
    {
        this.diagnosticsPublisher = diagnosticsPublisher;
    }

    public async Task<CompilationUnit?> GetDocumentAstAsync(Uri uri)
    {
        if (files.TryGetValue(uri, out var file))
        {
            return await file.GetAstAsync();
        }

        throw new ArgumentException($"Unknown document '{uri.AbsolutePath}'", nameof(uri));
    }

    private async Task SendDiagnostics(Uri uri)
    {
        var d = await files[uri].GetDiagnosticsAsync();
        if (d is not null)
        {
            await diagnosticsPublisher.SendDiagnosticsAsync(uri, d);
        }
    }

    public async Task OpenDocumentAsync(Uri uri, string text)
    {
        Debug.WriteLine($">> Document opened: {uri.AbsolutePath}");
        files[uri] = new ScriptFile(uri, text);
        await SendDiagnostics(uri);
    }

    public async Task UpdateDocumentAsync(Uri uri, string text)
    {
        Debug.WriteLine($">> Document updated: {uri.AbsolutePath}");
        if (files.TryGetValue(uri, out var file))
        {
            file.UpdateText(text);
            await SendDiagnostics(uri);
        }
    }

    public Task CloseDocumentAsync(Uri uri)
    {
        Debug.WriteLine($">> Document closed: {uri.AbsolutePath}");
        if (files.Remove(uri, out var file))
        {
            file.Dispose();
        }
        return Task.CompletedTask;
    }

    private class ScriptFile : IDisposable
    {
        private CancellationTokenSource? analyzeTaskCts;
        private Task analyzeTask;
        public Uri Uri { get; }
        public string Text { get; private set; }
        private DiagnosticsReport? Diagnostics { get; set; } = null;
        private CompilationUnit? Ast { get; set; } = null;
        
        public ScriptFile(Uri uri, string text)
        {
            Uri = uri;
            Text = text;
            StartAnalysis();
            Debug.Assert(analyzeTask is not null);
        }

        public void Dispose()
        {
            CancelAnalysis();
        }

        public void UpdateText(string text)
        {
            Text = text;
            StartAnalysis();
        }

        public async Task<CompilationUnit?> GetAstAsync()
        {
            await analyzeTask.ConfigureAwait(false);
            return Ast;
        }

        public async Task<DiagnosticsReport?> GetDiagnosticsAsync()
        {
            await analyzeTask.ConfigureAwait(false);
            return Diagnostics;
        }

        private void CancelAnalysis()
        {
            analyzeTaskCts?.Cancel();
            analyzeTaskCts?.Dispose();
        }

        private void StartAnalysis()
        {
            CancelAnalysis();
            analyzeTaskCts = new CancellationTokenSource();
            analyzeTask = AnalyzeAsync(analyzeTaskCts.Token);
        }

        private async Task AnalyzeAsync(CancellationToken ct)
        {
            (Diagnostics, Ast) = await Task.Run<(DiagnosticsReport, CompilationUnit)>(() =>
            {
                try
                {
                    var diagnosticsReport = new DiagnosticsReport();
                    var lexer = new Lexer(Uri.AbsolutePath, Text, diagnosticsReport);
                    var parser = new Parser(lexer, diagnosticsReport);
                    var compilationUnit = parser.ParseCompilationUnit();
                    ct.ThrowIfCancellationRequested();
                    var sema = new SemanticsAnalyzer(diagnosticsReport);
                    compilationUnit.Accept(sema);
                    return (diagnosticsReport, compilationUnit);
                }
                catch(OperationCanceledException e)
                {
                    return default;
                }
            }, ct);
        }
    }
}

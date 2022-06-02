namespace ScTools.LanguageServer.Services;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Semantics;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public interface ITextDocumentTracker
{
    CompilationUnit GetDocumentAst(Uri uri);
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

    public CompilationUnit GetDocumentAst(Uri uri)
    {
        if (files.TryGetValue(uri, out var file))
        {
            return file.CompilationUnit;
        }

        throw new ArgumentException($"Unknown document '{uri.AbsolutePath}'", nameof(uri));
    }

    public async Task OpenDocumentAsync(Uri uri, string text)
    {
        Debug.WriteLine($">> Document opened: {uri.AbsolutePath}");
        files[uri] = new ScriptFile(uri, text);
        await diagnosticsPublisher.SendDiagnosticsAsync(uri, files[uri].Diagnostics);
    }

    public async Task UpdateDocumentAsync(Uri uri, string text)
    {
        Debug.WriteLine($">> Document updated: {uri.AbsolutePath}");
        if (files.TryGetValue(uri, out var file))
        {
            file.UpdateText(text);
            await diagnosticsPublisher.SendDiagnosticsAsync(uri, files[uri].Diagnostics);
        }
    }

    public Task CloseDocumentAsync(Uri uri)
    {
        Debug.WriteLine($">> Document closed: {uri.AbsolutePath}");
        files.Remove(uri);
        return Task.CompletedTask;
    }

    private class ScriptFile
    {
        public Uri Uri { get; }
        public string Text { get; private set; }
        public DiagnosticsReport Diagnostics { get; private set; }
        public CompilationUnit CompilationUnit { get; private set; }

        public ScriptFile(Uri uri, string text)
        {
            Uri = uri;
            Text = text;
            ReAnalyze();

            Debug.Assert(Diagnostics is not null);
            Debug.Assert(CompilationUnit is not null);
        }

        public void UpdateText(string text)
        {
            Text = text;
            ReAnalyze();
        }

        private void ReAnalyze()
        {
            var diagnosticsReport = new DiagnosticsReport();
            var lexer = new Lexer(Uri.AbsolutePath, Text, diagnosticsReport);
            var parser = new Parser(lexer, diagnosticsReport);
            var compilationUnit = parser.ParseCompilationUnit();
            var sema = new SemanticsAnalyzer(diagnosticsReport);
            compilationUnit.Accept(sema);

            Diagnostics = diagnosticsReport;
            CompilationUnit = compilationUnit;
        }
    }
}

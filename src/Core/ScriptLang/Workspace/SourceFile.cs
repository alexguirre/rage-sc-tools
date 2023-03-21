namespace ScTools.ScriptLang.Workspace;

using ScTools.GameFiles;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Semantics;

using System.IO;

public class SourceFile : IDisposable
{
    private record AnalysisResult(DiagnosticsReport Diagnostics, SemanticsAnalyzer Semantics, CompilationUnit Ast);
    public record CompilationResult(SourceFile Source, DiagnosticsReport Diagnostics, SemanticsAnalyzer Semantics, CompilationUnit Ast, IScript? Script);

    private bool isDisposed;
    private CancellationTokenSource? analyzeTaskCts;
    private Task<AnalysisResult?>? analyzeTask;

    public Project Project { get; }
    public string Path { get; }
    public string Text { get; private set; }

    public SourceFile(Project project, string filePath, string text)
    {
        Project = project;
        Path = filePath;
        Text = text;
    }

    public void UpdateText(string text)
    {
        Text = text;
        CancelAnalysis();
    }

    public async Task<CompilationUnit?> GetAstAsync(CancellationToken cancellationToken = default)
    {
        var result = await AnalyzeAsync(cancellationToken).ConfigureAwait(false);
        return result?.Ast;
    }

    public async Task<DiagnosticsReport?> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var result = await AnalyzeAsync(cancellationToken).ConfigureAwait(false);
        return result?.Diagnostics;
    }

    public async Task<CompilationResult?> CompileAsync(CancellationToken cancellationToken = default)
    {
        var result = await AnalyzeAsync(cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        IScript? script = null;
        if (!result.Diagnostics.HasErrors)
        {
            script = await Task.Run(() => CodeGen.ScriptCompiler.Compile(result.Ast, Project.BuildConfiguration.Target), cancellationToken).ConfigureAwait(false);
        }
        return new(this, result.Diagnostics, result.Semantics, result.Ast, script);
    }

    private void CancelAnalysis()
    {
        analyzeTaskCts?.Cancel();
        analyzeTaskCts?.Dispose();
        analyzeTaskCts = null;
        analyzeTask = null;
    }

    private Task<AnalysisResult?> AnalyzeAsync(CancellationToken ct)
    {
        if (analyzeTask is not null)
        {
            // check if the task is already running
            return analyzeTask;
        }

        Debug.Assert(analyzeTaskCts is null);
        analyzeTaskCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        analyzeTask = RunAnalysisAsync(analyzeTaskCts.Token);
        return analyzeTask;
    }

    private Task<AnalysisResult?> RunAnalysisAsync(CancellationToken ct)
        => Task.Run(() =>
        {
            try
            {
                var diagnosticsReport = new DiagnosticsReport();
                var lexer = new Lexer(Path, Text, diagnosticsReport);
                var preprocessor = new Preprocessor(diagnosticsReport);
                Project.BuildConfiguration.Defines.ForEach(preprocessor.Define);
                var parser = new Parser(lexer, diagnosticsReport, preprocessor);
                var compilationUnit = parser.ParseCompilationUnit();
                ct.ThrowIfCancellationRequested();
                var semantics = new SemanticsAnalyzer(diagnosticsReport, usingResolver: Project);
                compilationUnit.Accept(semantics);
                ct.ThrowIfCancellationRequested();
                return new AnalysisResult(diagnosticsReport, semantics, compilationUnit);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }, ct);

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                CancelAnalysis();
            }
            isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static async Task<SourceFile> OpenAsync(Project project, string filePath, CancellationToken ct = default)
    {
        var text = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        return new SourceFile(project, filePath, text);
    }
}

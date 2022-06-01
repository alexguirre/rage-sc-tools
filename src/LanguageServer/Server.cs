namespace ScTools.LanguageServer;

using System;
using System.IO;
using System.Threading;

using Microsoft.VisualStudio.LanguageServer.Protocol;

using Newtonsoft.Json.Linq;

using StreamJsonRpc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ScTools.ScriptLang;
using ScTools.ScriptLang.Semantics;

internal sealed partial class Server : IDisposable
{
    private readonly Target target;
    private readonly HeaderDelimitedMessageHandler messageHandler;
    private readonly JsonRpc rpc;
    private readonly ManualResetEvent disconnectEvent = new(initialState: false);
    private readonly TraceSource traceSource;
    private readonly TraceSource rpcTraceSource;

    public Server(Stream sender, Stream receiver)
    {
        traceSource = new TraceSource("ScTools.LanguageServer.Server", SourceLevels.Verbose | SourceLevels.ActivityTracing);
        rpcTraceSource = new TraceSource("ScTools.LanguageServer.Server[RPC]", SourceLevels.Verbose | SourceLevels.ActivityTracing);

        target = new(this, rpcTraceSource);
        messageHandler = new(sender, receiver);
        rpc = new(messageHandler, target);
        rpc.Disconnected += OnRpcDisconnected;
        rpc.ActivityTracingStrategy = new CorrelationManagerTracingStrategy
        {
            TraceSource = rpcTraceSource,
        };
        rpc.TraceSource = rpcTraceSource;
        rpc.StartListening();
    }

    public void WaitForExit()
    {
        disconnectEvent.WaitOne();
    }

    public void Exit()
    {
        disconnectEvent.Set();
    }

    private void OnRpcDisconnected(object? sender, JsonRpcDisconnectedEventArgs e)
    {
        Exit();
    }

    public void Dispose()
    {
        rpc.Dispose();
        disconnectEvent.Dispose();
    }

    public void OnTextDocumentOpened(TextDocumentItem textDocument)
    {
        traceSource.TraceEvent(TraceEventType.Information, 0, $"Document opened: {textDocument.Uri.AbsolutePath}");

        SendDiagnostics(textDocument.Uri, textDocument.Text);
    }

    public void OnTextDocumentChanged(DidChangeTextDocumentParams didChangeTextDocument)
    {
        traceSource.TraceEvent(TraceEventType.Information, 0, $"Document changed: {didChangeTextDocument.TextDocument.Uri.AbsolutePath}");

        SendDiagnostics(didChangeTextDocument.TextDocument.Uri, didChangeTextDocument.ContentChanges[0].Text);
    }

    public void OnTextDocumentClosed(TextDocumentIdentifier textDocument)
    {
        traceSource.TraceEvent(TraceEventType.Information, 0, $"Document closed: {textDocument.Uri.AbsolutePath}");
    }

    public void SendDiagnostics(Uri uri, string sourceText)
    {
        var diagnosticsReport = new DiagnosticsReport();
        var lexer = new Lexer(uri.AbsolutePath, sourceText, diagnosticsReport);
        var parser = new Parser(lexer, diagnosticsReport);
        var compilationUnit = parser.ParseCompilationUnit();
        var sema = new SemanticsAnalyzer(diagnosticsReport);
        compilationUnit.Accept(sema);

        var param = new PublishDiagnosticParams
        {
            Uri = uri,
            Diagnostics = diagnosticsReport.AllDiagnostics.Select(d => d.ToLspDiagnostic()).ToArray(),
        };

        _ = SendMethodNotificationAsync(Methods.TextDocumentPublishDiagnostics, param);
    }

    private Task SendMethodNotificationAsync<TIn>(LspNotification<TIn> method, TIn param)
    {
        return rpc.NotifyWithParameterObjectAsync(method.Name, param);
    }
}

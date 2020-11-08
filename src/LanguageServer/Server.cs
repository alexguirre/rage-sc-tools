namespace ScTools.LanguageServer
{
    using System;
    using System.IO;
    using System.Threading;

    using Microsoft.VisualStudio.LanguageServer.Protocol;

    using Newtonsoft.Json.Linq;

    using StreamJsonRpc;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal sealed class Server : IDisposable
    {
        // based on https://github.com/microsoft/qsharp-compiler/tree/e1a43f3da021fc94d7214ea197ba3905ff96695c/src/QsCompiler/LanguageServer

        private readonly JsonRpc rpc;
        private readonly ManualResetEvent disconnectEvent;
        private ManualResetEvent? waitForInit;
        private readonly Dictionary<Uri, SourceFile> sourceFiles;

        private bool IsInitialized => waitForInit == null;
        public bool ReadyForExit { get; private set; } = false;

        public Server(Stream? sender, Stream? receiver)
        {
            waitForInit = new ManualResetEvent(false);

            rpc = new JsonRpc(sender, receiver, this);
            rpc.StartListening();

            disconnectEvent = new ManualResetEvent(false);
            rpc.Disconnected += (s, e) => disconnectEvent.Set();

            sourceFiles = new Dictionary<Uri, SourceFile>();

            waitForInit.Set();
        }

        public void Dispose()
        {
            sourceFiles.Clear();
            rpc.Dispose();
            disconnectEvent.Dispose();
            waitForInit?.Dispose();
        }

        public void WaitForShutdown()
        {
            disconnectEvent.WaitOne();
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public object Initialize(JToken arg)
        {
            var initialized = waitForInit?.WaitOne(30_000) ?? false;
            if (!initialized)
            {
                return new InitializeError { Retry = true };
            }

            var capabilities = new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Full,
                },
                DocumentSymbolProvider = true,
            };

            waitForInit = null;
            return new InitializeResult { Capabilities = capabilities };
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public object? Shutdown()
        {
            ReadyForExit = true;
            return null;
        }

        [JsonRpcMethod(Methods.ExitName)]
        public void Exit()
        {
            disconnectEvent.Set();
            this.Dispose();
        }

        [JsonRpcMethod(Methods.TextDocumentDidOpenName)]
        public object? OnTextDocumentDidOpen(JToken arg)
        {
            if (!IsInitialized)
            {
                return null;
            }

            var param = arg.ToObject<DidOpenTextDocumentParams>();
            var uri = param.TextDocument.Uri;
            sourceFiles[uri] = new SourceFile(uri);
            Console.WriteLine($"Opened '{uri}'");
            PublishDiagnostics(sourceFiles[uri].GetLspDiagnostics());
            return null;
        }

        [JsonRpcMethod(Methods.TextDocumentDidCloseName)]
        public object? OnTextDocumentDidClose(JToken arg)
        {
            if (!IsInitialized)
            {
                return null;
            }

            var param = arg.ToObject<DidCloseTextDocumentParams>();
            var uri = param.TextDocument.Uri;
            sourceFiles.Remove(uri);
            Console.WriteLine($"Closed '{uri}'");
            return null;
        }
        
        [JsonRpcMethod(Methods.TextDocumentDidChangeName)]
        public object? OnTextDocumentDidChange(JToken arg)
        {
            if (!IsInitialized)
            {
                return null;
            }

            var param = arg.ToObject<DidChangeTextDocumentParams>();
            var uri = param.TextDocument.Uri;
            Debug.Assert(param.ContentChanges.Length == 1);
            sourceFiles[uri].Source = param.ContentChanges[0].Text;
            Console.WriteLine($"Changed '{uri}'");
            PublishDiagnostics(sourceFiles[uri].GetLspDiagnostics());
            return null;
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName)]
        public object? OnTextDocumentSymbol(JToken arg)
        {
            if (!IsInitialized)
            {
                return null;
            }

            var param = arg.ToObject<DocumentSymbolParams>();
            var uri = param.TextDocument.Uri;
            Console.WriteLine($"Document symbol '{uri}'");
            return sourceFiles[uri].GetLspSymbols();
        }

        private void PublishDiagnostics(PublishDiagnosticParams @params)
        {
            rpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, @params).Wait();
        }
    }
}

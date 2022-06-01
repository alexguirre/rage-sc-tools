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

internal partial class Server
{
    private sealed class Target
    {
        private readonly Server server;
        private readonly TraceSource traceSource;

        public Target(Server server, TraceSource traceSource)
        {
            this.server = server;
            this.traceSource = traceSource;
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public InitializeResult Initialize(JToken arg)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, $"Received: {arg}");

            var capabilities = new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Full,
                },
                CompletionProvider = new CompletionOptions
                {
                    ResolveProvider = false,
                    TriggerCharacters = new[] { ".", ",", "(" },
                    AllCommitCharacters = new[] { " ", ".", "(", ")" }
                },
                SignatureHelpProvider = new SignatureHelpOptions
                {
                    TriggerCharacters = new[] { "(", "," },
                    RetriggerCharacters = new[] { ")" },
                },
                //DocumentHighlightProvider = true,
                //DocumentSymbolProvider = true,
                HoverProvider = true,
            };

            var result = new InitializeResult { Capabilities = capabilities };

            traceSource.TraceEvent(TraceEventType.Information, 0, $"Sending: {JToken.FromObject(result)}");
            return result;
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public void Initialized(JToken arg)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, $"Received: {arg}");
        }

        [JsonRpcMethod(Methods.TextDocumentDidOpenName)]
        public void OnTextDocumentOpened(JToken arg)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, $"Received: {arg}");
            var param = arg.ToObject<DidOpenTextDocumentParams>() ?? throw new ArgumentException("Invalid argument", nameof(arg));

            server.OnTextDocumentOpened(param.TextDocument);
        }

        [JsonRpcMethod(Methods.TextDocumentDidChangeName)]
        public void OnTextDocumentChanged(JToken arg)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, $"Received: {arg}");
            var param = arg.ToObject<DidChangeTextDocumentParams>() ?? throw new ArgumentException("Invalid argument", nameof(arg));

            server.OnTextDocumentChanged(param);
        }

        [JsonRpcMethod(Methods.TextDocumentDidCloseName)]
        public void OnTextDocumentClosed(JToken arg)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, $"Received: {arg}");
            var param = arg.ToObject<DidCloseTextDocumentParams>() ?? throw new ArgumentException("Invalid argument", nameof(arg));

            server.OnTextDocumentClosed(param.TextDocument);
        }

        [JsonRpcMethod(Methods.TextDocumentHoverName)]
        public Hover OnHover(JToken arg)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, $"Received: {arg}");
            var param = arg.ToObject<TextDocumentPositionParams>() ?? throw new ArgumentException("Invalid argument", nameof(arg));

            var result = new Hover
            {
                Contents = new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = "This is a **hover**!",
                },
                Range = new()
                {
                    Start = param.Position,
                    End = new(param.Position.Line, param.Position.Character + 1),
                },
            };

            traceSource.TraceEvent(TraceEventType.Information, 0, $"Sending: {JToken.FromObject(result)}");
            return result;
        }
    }
}

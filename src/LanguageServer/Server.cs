namespace ScTools.LanguageServer
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
    using LspDiagnostic = Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic;

    using Newtonsoft.Json.Linq;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Semantics.Symbols;

    using StreamJsonRpc;

    internal sealed class Server : IDisposable
    {
        // based on https://github.com/microsoft/qsharp-compiler/tree/e1a43f3da021fc94d7214ea197ba3905ff96695c/src/QsCompiler/LanguageServer

        private readonly JsonRpc rpc;
        private readonly ManualResetEvent disconnectEvent;
        private ManualResetEvent? waitForInit;

        private bool IsInitialized => waitForInit == null;
        public bool ReadyForExit { get; private set; } = false;

        public Server(Stream? sender, Stream? receiver)
        {
            waitForInit = new ManualResetEvent(false);

            rpc = new JsonRpc(sender, receiver, this);
            rpc.StartListening();

            disconnectEvent = new ManualResetEvent(false);
            rpc.Disconnected += (s, e) => disconnectEvent.Set();

            waitForInit.Set();
        }

        public void Dispose()
        {
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
            Console.WriteLine($"Opened '{param.TextDocument.Uri}'");
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
            Console.WriteLine($"Closed '{param.TextDocument.Uri}'");
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
            var path = param.TextDocument.Uri.AbsolutePath;
            Console.WriteLine($"Document symbol '{path}'");
            using var reader = new StreamReader(path);
            var module = Module.Parse(reader, path);

            var symbols = module.SymbolTable.Symbols.Select(s =>
            {
                return new SymbolInformation
                {
                    ContainerName = "global",
                    Kind = s switch
                    {
                        TypeSymbol _ => SymbolKind.Struct,
                        VariableSymbol _ => SymbolKind.Variable,
                        FunctionSymbol _ => SymbolKind.Function,
                        _ => throw new NotImplementedException(),
                    },
                    Location = new Location
                    {
                        Range = s.Source.IsUnknown ? new Range() : new Range
                        {
                            Start = new Position(s.Source.Start.Line, s.Source.Start.Column),
                            End = new Position(s.Source.End.Line, s.Source.End.Column),
                        },
                        Uri = s.Source.IsUnknown ? null : param.TextDocument.Uri,
                    },
                    Name = s.Name,
                };
                //return new DocumentSymbol
                //{
                //    Kind = s switch
                //    {
                //        TypeSymbol _ => SymbolKind.Struct,
                //        VariableSymbol _ => SymbolKind.Variable,
                //        FunctionSymbol _ => SymbolKind.Function,
                //        _ => throw new NotImplementedException(),
                //    },
                //    Name = s.Name,
                //    Detail = "The detail",
                //    Range = s.Source.IsUnknown ? new Range() : new Range
                //    {
                //        Start = new Position(s.Source.Start.Line, s.Source.Start.Column),
                //        End = new Position(s.Source.End.Line, s.Source.End.Column),
                //    },
                //    SelectionRange = s.Source.IsUnknown ? new Range() : new Range
                //    {
                //        Start = new Position(s.Source.Start.Line, s.Source.Start.Column),
                //        End = new Position(s.Source.End.Line, s.Source.End.Column),
                //    },
                //};
            }).ToArray();

            var diagnostics = new PublishDiagnosticParams
            {
                Uri = param.TextDocument.Uri,
                Diagnostics = module.Diagnostics.AllDiagnostics.Select(d =>
                {
                    return new LspDiagnostic
                    {
                        Code = "SC0000",
                        Source = "the source",
                        Message = d.Message,
                        Severity = d switch
                        {
                            ErrorDiagnostic _ => DiagnosticSeverity.Error,
                            WarningDiagnostic _ => DiagnosticSeverity.Warning,
                            _ => throw new NotImplementedException(),
                        },
                        Range = d.Source.IsUnknown ? new Range() : new Range
                        {
                            Start = new Position(d.Source.Start.Line - 1, d.Source.Start.Column - 1),
                            End = new Position(d.Source.End.Line - 1, d.Source.End.Column - 1),
                        },
                    };
                }).ToArray(),
            };
            PublishDiagnostics(diagnostics);

            return symbols;
        }

        private void PublishDiagnostics(PublishDiagnosticParams @params)
        {
            rpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, @params).Wait();
        }
    }
}

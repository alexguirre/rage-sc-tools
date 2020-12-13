namespace ScTools.LanguageServer
{
    using System;
    using System.IO;
    using System.Threading;

    using Microsoft.VisualStudio.LanguageServer.Protocol;

    using Newtonsoft.Json.Linq;

    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Semantics.Symbols;
    using StreamJsonRpc;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

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
                CompletionProvider = new CompletionOptions
                {
                    TriggerCharacters = new[] { ".", ",", "(" },
                    AllCommitCharacters = new[] { " ", ".", ",", "(", ")" }
                },
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

        [JsonRpcMethod(Methods.TextDocumentCompletionName)]
        public object? OnTextDocumentCompletion(JToken arg)
        {
            var param = arg.ToObject<CompletionParams>();
            Console.WriteLine($"Document completion '{param.TextDocument.Uri}'");

            var file = sourceFiles[param.TextDocument.Uri];
            if (param.Context != null && param.Context.TriggerKind == CompletionTriggerKind.TriggerCharacter)
            {
                switch (param.Context.TriggerCharacter)
                {
                    case ".":
                        return GetDotCompletions(file, param.Position) ?? Array.Empty<CompletionItem>();
                }
            }

            return GetDefaultCompletions(file, param.Position) ?? Array.Empty<CompletionItem>();
        }

        private CompletionItem[]? GetDefaultCompletions(SourceFile file, Position position)
        {
            var scope = file.FindScopeAt(position.ToSourceLocation());
            if (scope == null)
            {
                return null;
            }

            static IEnumerable<ISymbol> AllSymbols(SymbolTable table)
                => table.Parent != null ?
                        AllSymbols(table.Parent).Concat(table.Symbols) :
                        table.Symbols.Concat(table.Imports.SelectMany(i => i.Symbols)).Concat(SymbolTable.BuiltIns);

            return AllSymbols(scope).Select(s => new CompletionItem
            {
                Label = s.Name,
                Detail = SourceFile.GetLspDetail(s),
                Kind = s switch
                {
                    TypeSymbol _ => CompletionItemKind.Struct,
                    VariableSymbol _ => CompletionItemKind.Variable,
                    FunctionSymbol _ => CompletionItemKind.Function,
                    _ => throw new NotImplementedException(),
                },
            }).ToArray();
        }

        private CompletionItem[]? GetDotCompletions(SourceFile file, Position position)
        {
            var scope = file.FindScopeAt(position.ToSourceLocation());
            if (scope == null)
            {
                return null;
            }

            var prevWord = GetPrevWord(file.Source, position);
            if (prevWord == null)
            {
                return null;
            }

            var identifiers = prevWord.Split('.').Select(s => s.Trim()).ToArray();
            if (identifiers.Length == 0)
            {
                return null;
            }

            var varSymbol = scope.Lookup(identifiers[0]) as VariableSymbol;
            if (varSymbol?.Type is StructType structTy)
            {
                foreach (var fieldName in identifiers.Skip(1))
                {
                    if (!structTy.HasField(fieldName))
                    {
                        return null;
                    }

                    if (structTy.TypeOfField(fieldName) is StructType fieldStructTy)
                    {
                        structTy = fieldStructTy;
                    }
                    else
                    {
                        return null;
                    }
                }

                return structTy.Fields.Select(f => new CompletionItem
                {
                    Label = f.Name,
                    Detail = f.Type.ToString(),
                    Kind = CompletionItemKind.Field
                }).ToArray();
            }

            return null;
        }

        private static string? GetLine(string text, Position position)
        {
            int line = 0, lineStartIndex = 0;
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\n')
                {
                    if (line == position.Line)
                    {
                        return text[lineStartIndex..i];
                    }
                    else
                    {
                        line++;
                        lineStartIndex = i + 1;
                    }
                }
            }

            return null;
        }

        private static string? GetPrevWord(string text, Position position)
        {
            var line = GetLine(text, position);
            if (line == null)
            {
                return null;
            }

            var end = position.Character;
            while (IsDelimeter(line[end - 1]) || line[end - 1] == '.') { end--; }

            var start = 0;
            for (int i = end - 1; i >= 0; i--)
            {
                if (IsDelimeter(line[i]))
                {
                    start = i + 1;
                    break;
                }
            }

            return line[start..end].Trim();

            static bool IsDelimeter(char c)
                => c != '_' && c != ' ' && c != '.' && !char.IsLetterOrDigit(c);
        }

        private void PublishDiagnostics(PublishDiagnosticParams @params)
        {
            rpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, @params).Wait();
        }
    }
}

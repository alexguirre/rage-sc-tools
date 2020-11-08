namespace ScTools.LanguageServer
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
    using LspDiagnostic = Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Semantics.Symbols;

    internal sealed class SourceFile
    {
        private string source = "";
        private Module? module;

        public Uri Uri { get; }
        public string Path { get; }
        public string Source
        {
            get => source;
            set
            {
                source = value;
                OnSourceChanged(value);
            }
        }

        public SourceFile(Uri uri)
        {
            Uri = uri;
            Path = uri.AbsolutePath;
            Source = File.ReadAllText(Path);
        }

        public PublishDiagnosticParams GetLspDiagnostics()
            => new PublishDiagnosticParams
            {
                Uri = Uri,
                Diagnostics = module?.Diagnostics.AllDiagnostics.Select(d =>
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
                        Range = ToLspRange(d.Source),
                    };
                }).ToArray() ?? Array.Empty<LspDiagnostic>(),
            };

        public SymbolInformation[] GetLspSymbols()
            => module?.SymbolTable.Symbols.Select(s => new SymbolInformation
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
                    Range = ToLspRange(s.Source),
                    Uri = Uri,
                },
                Name = s.Name,
            }
            ).ToArray() ?? Array.Empty<SymbolInformation>();

        private void OnSourceChanged(string newSource)
        {
            using var reader = new StringReader(newSource);
            module = Module.Parse(reader, Path);
        }


        private static LspRange ToLspRange(SourceRange r)
            => r.IsUnknown ? new LspRange() : new LspRange
            {
                Start = new Position(r.Start.Line - 1, r.Start.Column - 1),
                End = new Position(r.End.Line - 1, r.End.Column - 1),
            };
    }
}

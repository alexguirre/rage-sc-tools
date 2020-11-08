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
    using ScTools.ScriptLang.Semantics;
    using System.Text;
    using System.Diagnostics;
    using System.Collections.Generic;

    internal sealed class SourceFile
    {
        private string source = "";
        private Module? module;
        private DocumentSymbol[]? cachedSymbols;

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

        private void OnSourceChanged(string newSource)
        {
            using var reader = new StringReader(newSource);
            module = Module.Parse(reader, Path);
            if (!module.Diagnostics.HasErrors)
            {
                cachedSymbols = null;
            }
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

        public DocumentSymbol[] GetLspSymbols()
            => cachedSymbols ??= module?.SymbolTable.Symbols.Where(s => !s.Source.IsUnknown).Select(ToLspSymbol).ToArray() ?? Array.Empty<DocumentSymbol>();

        private DocumentSymbol ToLspSymbol(ISymbol symbol)
            => new DocumentSymbol
            {
                Kind = symbol switch
                {
                    TypeSymbol _ => SymbolKind.Struct,
                    VariableSymbol _ => SymbolKind.Variable,
                    FunctionSymbol _ => SymbolKind.Function,
                    _ => throw new NotImplementedException(),
                },
                Range = ToLspRange(symbol.Source),
                SelectionRange = ToLspRange(symbol.Source),
                Name = symbol.Name,
                Detail = GetLspDetail(symbol),
                Children = GetLspSymbolChildren(symbol),
            };

        private static string GetLspDetail(ISymbol symbol)
        {
            return symbol switch
            {
                TypeSymbol _ => string.Empty,
                VariableSymbol v => VariableDetail(v),
                FunctionSymbol f => FunctionDetail(f),
                _ => throw new NotImplementedException(),
            };

            static string VariableDetail(VariableSymbol v)
            {
                var sb = new StringBuilder(v.Type.ToString());
                sb.Append(' ');
                switch (v.Kind)
                {
                    case VariableKind.Static:
                        sb.Append("static");
                        break;
                    case VariableKind.Local:
                        sb.Append("local");
                        break;
                    case VariableKind.LocalArgument:
                        sb.Append("parameter");
                        break;
                }
                return sb.ToString();
            }

            static string FunctionDetail(FunctionSymbol f)
            {
                // TODO: include parameter names in function symbol detail
                var sb = new StringBuilder();
                if (!f.IsProcedure)
                {
                    sb.Append(f.Type.ReturnType!.ToString());
                    sb.Append(' ');
                }
                sb.Append("(");
                sb.AppendJoin(", ", f.Type.Parameters);
                sb.Append(")");
                return sb.ToString();
            }
        }

        private DocumentSymbol[] GetLspSymbolChildren(ISymbol symbol)
        {
            Debug.Assert(module != null);

            switch (symbol)
            {
                case TypeSymbol t when t.Type is StructType structTy:
                    return structTy.Fields.Select(f => new DocumentSymbol
                    {
                        Kind = SymbolKind.Field,
                        // TODO: report source range of struct fields
                        Range = ToLspRange(t.Source),
                        SelectionRange = ToLspRange(t.Source),
                        Name = f.Name,
                        Detail = f.Type.ToString(),
                        Children = Array.Empty<DocumentSymbol>(),
                    }).ToArray();
                case FunctionSymbol f when !f.IsNative:
                    var funcScope = module.SymbolTable.GetScope(f);
                    return GetAllSymbols(funcScope).Select(ToLspSymbol).ToArray();
                default:
                    return Array.Empty<DocumentSymbol>();
            }
        }

        /// <summary>
        /// Gets all symbols in the table, including symbols in children tabes.
        /// </summary>
        private IEnumerable<ISymbol> GetAllSymbols(SymbolTable symbols)
        {
            return symbols.Symbols.Concat(symbols.Children.SelectMany(kvp => GetAllSymbols(kvp.Table)));
        }

        private static LspRange ToLspRange(SourceRange r)
            => r.IsUnknown ? new LspRange() : new LspRange
            {
                Start = new Position(r.Start.Line - 1, r.Start.Column - 1),
                End = new Position(r.End.Line - 1, r.End.Column - 1),
            };
    }
}

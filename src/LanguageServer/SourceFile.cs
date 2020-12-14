namespace ScTools.LanguageServer
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.VisualStudio.LanguageServer.Protocol;
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
        private readonly Compilation compilation;
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
        public SymbolTable? GlobalScope => compilation.MainModule?.SymbolTable;

        public SourceFile(Uri uri)
        {
            Uri = uri;
            Path = uri.AbsolutePath;
            var dir = System.IO.Path.GetDirectoryName(Path)!;
            compilation = new Compilation
            {
                SourceResolver = new DefaultSourceResolver(dir),
            };
            Source = File.ReadAllText(Path);
        }

        private void OnSourceChanged(string newSource)
        {
            using var reader = new StringReader(newSource);
            compilation.SetMainModule(reader, Path);
            compilation.PerformPendingAnalysis();
            Console.WriteLine($"Parsed '{Path}' (global symbols: {compilation.MainModule!.SymbolTable?.Symbols.Count()})");
            if (!compilation.MainModule!.Diagnostics.HasErrors)
            {
                cachedSymbols = null;
            }
        }

        public SymbolTable? FindScopeAt(SourceLocation location)
        {
            if (compilation.MainModule?.SymbolTable == null)
            {
                return null;
            }

            return RecursiveFind(compilation.MainModule.SymbolTable, location);

            static SymbolTable? RecursiveFind(SymbolTable symbols, SourceLocation location)
            {
                if (symbols.AstNode.Source.Contains(location))
                {
                    foreach (var childTable in symbols.Children)
                    {
                        var t = RecursiveFind(childTable, location);
                        if (t != null)
                        {
                            return t;
                        }
                    }

                    return symbols;
                }

                return null;
            }
        }

        public PublishDiagnosticParams GetLspDiagnostics()
            => new PublishDiagnosticParams
            {
                Uri = Uri,
                Diagnostics = compilation.MainModule?.Diagnostics.AllDiagnostics.Select(d =>
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
                        Range = d.Source.ToLspRange(),
                    };
                }).ToArray() ?? Array.Empty<LspDiagnostic>(),
            };

        public DocumentSymbol[] GetLspSymbols()
            => cachedSymbols ??= compilation.MainModule?.SymbolTable?.Symbols.Where(s => !s.Source.IsUnknown).Select(ToLspSymbol).ToArray() ?? Array.Empty<DocumentSymbol>();

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
                Range = symbol.Source.ToLspRange(),
                SelectionRange = symbol.Source.ToLspRange(),
                Name = symbol.Name,
                Detail = GetLspDetail(symbol),
                Children = GetLspSymbolChildren(symbol),
            };

        public static string GetLspDetail(ISymbol symbol)
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
                var addComma = false;
                foreach (var (type, name) in f.Type.Parameters)
                {
                    if (addComma)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(type);
                    if (name != null)
                    {
                        sb.Append(' ');
                        sb.Append(name);
                    }
                    addComma = true;
                }
                sb.Append(")");
                return sb.ToString();
            }
        }

        private DocumentSymbol[] GetLspSymbolChildren(ISymbol symbol)
        {
            Debug.Assert(compilation.MainModule != null);

            switch (symbol)
            {
                case TypeSymbol t when t.Type is StructType structTy:
                    return structTy.Fields.Select(f => new DocumentSymbol
                    {
                        Kind = SymbolKind.Field,
                        // TODO: report source range of struct fields
                        Range = t.Source.ToLspRange(),
                        SelectionRange = t.Source.ToLspRange(),
                        Name = f.Name,
                        Detail = f.Type.ToString(),
                        Children = Array.Empty<DocumentSymbol>(),
                    }).ToArray();
                case FunctionSymbol f when !f.IsNative:
                    Debug.Assert(compilation.MainModule.SymbolTable != null);
                    var funcScope = compilation.MainModule.SymbolTable.GetScope(f.AstBlock!);
                    return GetAllSymbols(funcScope)!.Select(ToLspSymbol).ToArray();
                default:
                    return Array.Empty<DocumentSymbol>();
            }
        }

        /// <summary>
        /// Gets all symbols in the table, including symbols in children tabes.
        /// </summary>
        private IEnumerable<ISymbol> GetAllSymbols(SymbolTable symbols)
        {
            return symbols.Symbols.Concat(symbols.Children.SelectMany(t => GetAllSymbols(t)));
        }
    }
}

#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics.Binding;
    using ScTools.ScriptLang.Semantics.Symbols;

    public static partial class SemanticAnalysis
    {
        public static (DiagnosticsReport, SymbolTable, BoundModule) Visit(Root root, string filePath)
        {
            var diagnostics = new DiagnosticsReport();
            var symbols = new SymbolTable();
            AddBuiltIns(symbols);
            var pass1 = new FirstPass(diagnostics, filePath, symbols);
            pass1.Run(root);
            new SecondPass(diagnostics, filePath, symbols).Run(root);

            var binderPass = new Binder(diagnostics, filePath, symbols, pass1.StaticVarsTotalSize);
            binderPass.Run(root);

            return (diagnostics, symbols, binderPass.Module);
        }

        private static void AddBuiltIns(SymbolTable symbols)
        {
            var fl = new BasicType(BasicTypeCode.Float);
            symbols.Add(new TypeSymbol("INT", SourceRange.Unknown, new BasicType(BasicTypeCode.Int)));
            symbols.Add(new TypeSymbol("FLOAT", SourceRange.Unknown, fl));
            symbols.Add(new TypeSymbol("BOOL", SourceRange.Unknown, new BasicType(BasicTypeCode.Bool)));
            symbols.Add(new TypeSymbol("STRING", SourceRange.Unknown, new BasicType(BasicTypeCode.String)));
            symbols.Add(new TypeSymbol("VEC3", SourceRange.Unknown, new StructType("VEC3",
                                                                        new Field(fl, "x"),
                                                                        new Field(fl, "y"),
                                                                        new Field(fl, "z"))));
        }

        private abstract class Pass : AstVisitor
        {
            public DiagnosticsReport Diagnostics { get; set; }
            public string FilePath { get; set; }
            public SymbolTable Symbols { get; set; }

            public Pass(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols)
                => (Diagnostics, FilePath, Symbols) = (diagnostics, filePath, symbols);

            public void Run(Root root)
            {
                root.Accept(this);
                OnEnd();
            }

            protected virtual void OnEnd() { }

            protected Type TryResolveType(string typeName, SourceRange source)
            {
                var unresolved = new UnresolvedType(typeName);
                var resolved = unresolved.Resolve(Symbols);
                if (resolved == null)
                {
                    Diagnostics.AddError(FilePath, $"Unknown type '{typeName}'", source);
                }

                return resolved ?? unresolved;
            }

            protected Type? TypeOf(Expression expr) => expr.Accept(new TypeOf(Diagnostics, FilePath, Symbols));
        }
    }
}

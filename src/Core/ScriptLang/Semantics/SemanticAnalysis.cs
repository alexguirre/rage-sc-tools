#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics.Binding;
    using ScTools.ScriptLang.Semantics.Symbols;

    public static partial class SemanticAnalysis
    {
        public static void DoFirstPass(Root root, string filePath, SymbolTable symbols, IUsingModuleResolver? usingResolver, DiagnosticsReport diagnostics)
            => new FirstPass(diagnostics, filePath, symbols, usingResolver).Run(root);

        public static void DoSecondPass(Root root, string filePath, SymbolTable symbols, DiagnosticsReport diagnostics)
            => new SecondPass(diagnostics, filePath, symbols).Run(root);

        public static BoundModule DoBinding(Root root, string filePath, SymbolTable symbols, DiagnosticsReport diagnostics)
        {
            var pass = new Binder(diagnostics, filePath, symbols);
            pass.Run(root);
            return pass.Module;
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

            protected Type TryResolveType(Ast.Type type)
            {
                var unresolved = new UnresolvedType(type.Name, type.IsReference);
                var resolved = unresolved.Resolve(Symbols);
                if (resolved == null)
                {
                    Diagnostics.AddError(FilePath, $"Unknown type '{type.Name}'", type.Source);
                }

                return resolved ?? unresolved;
            }

            protected UnresolvedType UnresolvedTypeFromAst(Ast.Type type)
                => new UnresolvedType(type.Name, type.IsReference);

            protected Type? TypeOf(Expression expr) => expr.Accept(new TypeOf(Diagnostics, FilePath, Symbols));
        }
    }
}

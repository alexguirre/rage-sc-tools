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
            new FirstPass(diagnostics, filePath, symbols).Run(root);
            new SecondPass(diagnostics, filePath, symbols).Run(root);

            var binderPass = new Binder(diagnostics, filePath, symbols);
            binderPass.Run(root);

            return (diagnostics, symbols, binderPass.Module);
        }

        private static void AddBuiltIns(SymbolTable symbols)
        {
            // basic types
            var flTy = new BasicType(BasicTypeCode.Float);
            var intTy = new BasicType(BasicTypeCode.Int);
            symbols.Add(new TypeSymbol("INT", SourceRange.Unknown, intTy));
            symbols.Add(new TypeSymbol("FLOAT", SourceRange.Unknown, flTy));
            symbols.Add(new TypeSymbol("BOOL", SourceRange.Unknown, new BasicType(BasicTypeCode.Bool)));
            symbols.Add(new TypeSymbol("STRING", SourceRange.Unknown, new BasicType(BasicTypeCode.String)));

            // struct types
            static Field F(Type ty, string name) => new Field(ty, name);
            var entityIndexTy = new StructType("ENTITY_INDEX", F(intTy, "value"));
            var structTypes = new[]
            {
                new StructType("VEC3", F(flTy, "x"), F(flTy, "y"), F(flTy, "z")),
                new StructType("PLAYER_INDEX", F(intTy, "value")),
                entityIndexTy,
                new StructType("PED_INDEX", F(entityIndexTy, "base")),
                new StructType("VEHICLE_INDEX", F(entityIndexTy, "base")),
                new StructType("OBJECT_INDEX", F(entityIndexTy, "base")),
                new StructType("CAMERA_INDEX", F(intTy, "value")),
            };

            foreach (var structTy in structTypes)
            {
                symbols.Add(new TypeSymbol(structTy.Name!, SourceRange.Unknown, structTy));
            }
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

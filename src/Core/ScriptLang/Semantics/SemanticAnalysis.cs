#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Linq;

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

            protected Type ResolveTypeFromDecl(Declaration varDecl)
            {
                bool unresolved = false;
                return Resolve(TypeFromDecl(varDecl), varDecl.Source, ref unresolved);
            }

            protected Type TypeFromDecl(Declaration decl)
            {
                var unresolved = new UnresolvedType(decl.Type);
                return decl != null ? TypeFromDeclarator(decl.Declarator, unresolved) : unresolved;
            }

            protected Type TypeFromDeclarator(Declarator decl, Type baseType)
            {
                var source = decl.Source;
                var ty = baseType;
                while (decl is not SimpleDeclarator)
                {
                    switch (decl)
                    {
                        case ArrayDeclarator when ty is RefType:
                            Diagnostics.AddError(FilePath, $"Array of references is not valid", source);
                            return baseType;
                        case ArrayDeclarator d:
                            ty = new UnresolvedArrayType(ty, d.Length);
                            decl = d.Inner;
                            break;

                        case RefDeclarator when ty is RefType:
                            Diagnostics.AddError(FilePath, $"Reference to reference is not valid", source);
                            return baseType;
                        case RefDeclarator d:
                            ty = new RefType(ty);
                            decl = d.Inner;
                            break;

                        default: throw new NotImplementedException();
                    };
                }

                return ty;
            }

            protected void ResolveStruct(StructType struc, SourceRange source, ref bool unresolved)
            {
                // TODO: be more specific with SourceRange for structs fields
                for (int i = 0; i < struc.Fields.Count; i++)
                {
                    var f = struc.Fields[i];
                    var newType = Resolve(f.Type, source, ref unresolved);
                    if (IsCyclic(newType, struc))
                    {
                        Diagnostics.AddError(FilePath, $"Circular type reference in '{struc.Name}'", source);
                        unresolved = true;
                    }
                    else
                    {
                        struc.Fields[i] = new Field(newType, f.Name);
                    }
                }

                static bool IsCyclic(Type t, StructType orig)
                {
                    if (t == orig)
                    {
                        return true;
                    }
                    else if (t is StructType s)
                    {
                        return s.Fields.Any(f => IsCyclic(f.Type, orig));
                    }

                    return false;
                }
            }

            protected void ResolveFunc(ExplicitFunctionType func, SourceRange source, ref bool unresolved)
            {
                // TODO: be more specific with SourceRange for funcs return type and parameters
                if (func.ReturnType != null)
                {
                    func.ReturnType = Resolve(func.ReturnType, source, ref unresolved);
                }

                for (int i = 0; i < func.Parameters.Count; i++)
                {
                    var p = func.Parameters[i];
                    func.Parameters[i] = (Resolve(p.Type, source, ref unresolved), p.Name);
                }
            }

            protected void ResolveArray(ArrayType arr, SourceRange source, ref bool unresolved)
            {
                arr.ItemType = Resolve(arr.ItemType, source, ref unresolved);
            }

            protected void ResolveRef(RefType refTy, SourceRange source, ref bool unresolved)
            {
                refTy.ElementType = Resolve(refTy.ElementType, source, ref unresolved);
            }

            protected Type Resolve(Type t, SourceRange source, ref bool unresolved)
            {
                switch (t)
                {
                    case UnresolvedType or UnresolvedArrayType:
                    {
                        var newType = t.Resolve(Symbols, Diagnostics, FilePath);
                        if (newType == null)
                        {
                            unresolved = true;
                            return t;
                        }
                        else
                        {
                            return newType;
                        }
                    }
                    case RefType ty: ResolveRef(ty, source, ref unresolved); return ty;
                    case StructType ty: ResolveStruct(ty, source, ref unresolved); return ty;
                    case ExplicitFunctionType ty: ResolveFunc(ty, source, ref unresolved); return ty;
                    case ArrayType ty: ResolveArray(ty, source, ref unresolved); return ty;
                    default: return t;
                }
            }

            protected Type? TypeOf(Expression expr) => expr.Accept(new TypeOf(Diagnostics, FilePath, Symbols));
        }
    }
}

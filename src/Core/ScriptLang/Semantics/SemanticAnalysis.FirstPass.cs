#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics.Symbols;

    public static partial class SemanticAnalysis
    {
        /// <summary>
        /// Register global symbols (structs, static variable, procedures and functions)
        /// </summary>
        private sealed class FirstPass : Pass
        {
            public FirstPass(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols)
                : base(diagnostics, filePath, symbols)
            { }

            protected override void OnEnd()
            {
                bool allResolved = ResolveTypes();
                if (allResolved)
                {
                    AllocateStaticVars();
                }
            }

            // returns whether all types where resolved
            private bool ResolveTypes()
            {
                bool anyUnresolved = false;

                foreach (var symbol in Symbols.Symbols)
                {
                    switch (symbol)
                    {
                        case VariableSymbol s: s.Type = Resolve(s.Type, s.Source); break;
                        case FunctionSymbol s: ResolveFunc(s.Type, s.Source); break;
                        case TypeSymbol s when s.Type is StructType struc: ResolveStruct(struc, s.Source); break;
                        case TypeSymbol s when s.Type is FunctionType func: ResolveFunc(func, s.Source); break;
                    }
                }

                return !anyUnresolved;

                void ResolveStruct(StructType struc, SourceRange source)
                {
                    // TODO: be more specific with SourceRange for structs fields
                    for (int i = 0; i < struc.Fields.Count; i++)
                    {
                        var f = struc.Fields[i];
                        var newType = Resolve(f.Type, source);
                        if (IsCyclic(newType, struc))
                        {
                            Diagnostics.AddError(FilePath, $"Circular type reference in '{struc.Name}'", source);
                            anyUnresolved |= true;
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

                void ResolveFunc(FunctionType func, SourceRange source)
                {
                    // TODO: be more specific with SourceRange for funcs return type and parameters
                    if (func.ReturnType != null)
                    {
                        func.ReturnType = Resolve(func.ReturnType, source);
                    }
 
                    for (int i = 0; i < func.Parameters.Count; i++)
                    {
                        func.Parameters[i] = Resolve(func.Parameters[i], source);
                    }
                }

                Type Resolve(Type t, SourceRange source)
                {
                    if (t is UnresolvedType u)
                    {
                        var newType = u.Resolve(Symbols);
                        if (newType == null)
                        {
                            Diagnostics.AddError(FilePath, $"Unknown type '{u.TypeName}'", source);
                            anyUnresolved |= true;
                        }
                        else
                        {
                            return newType;
                        }
                    }

                    return t;
                }
            }

            private void AllocateStaticVars()
            {
                Debug.Assert(Symbols.Parent == null, $"{nameof(AllocateStaticVars)} must be called from the global scope");

                int location = 0;
                foreach (var s in Symbols.Symbols.Where(sym => sym is VariableSymbol { Kind: VariableKind.Static })
                                                 .Cast<VariableSymbol>()
                                                 .Reverse()) // reverse to allocate in the order they are declared, the symbol table enumerates them from bottom to top
                {
                    Debug.Assert(!s.IsAllocated);

                    s.Location = location;
                    location += s.Type.SizeOf;
                }

                // TODO: return total size of static vars
            }

            private FunctionType CreateUnresolvedFunctionType(Ast.Type? returnType, IEnumerable<VariableDeclaration> parameters)
            {
                var r = returnType != null ? new UnresolvedType(returnType.Name) : null;
                return new FunctionType(r, parameters.Select(p => new UnresolvedType(p.Type.Name)));
            }

            public override void VisitFunctionStatement(FunctionStatement node)
            {
                Symbols.Add(new FunctionSymbol(node.Name, node.Source, CreateUnresolvedFunctionType(node.ReturnType, node.ParameterList.Parameters)));
            }

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                Symbols.Add(new FunctionSymbol(node.Name, node.Source, CreateUnresolvedFunctionType(null, node.ParameterList.Parameters)));
            }

            public override void VisitFunctionPrototypeStatement(FunctionPrototypeStatement node)
            {
                var func = CreateUnresolvedFunctionType(node.ReturnType, node.ParameterList.Parameters);

                Symbols.Add(new TypeSymbol(node.Name, node.Source, func));
            }

            public override void VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node)
            {
                var func = CreateUnresolvedFunctionType(null, node.ParameterList.Parameters);

                Symbols.Add(new TypeSymbol(node.Name, node.Source, func));
            }

            public override void VisitStaticVariableStatement(StaticVariableStatement node)
            {
                Debug.Assert(node.Variable.Initializer == null, "Static initializers are not supported");

                Symbols.Add(new VariableSymbol(node.Variable.Declaration.Name,
                                               node.Source,
                                               new UnresolvedType(node.Variable.Declaration.Type.Name),
                                               VariableKind.Static));
            }

            public override void VisitStructStatement(StructStatement node)
            {
                var struc = new StructType(node.Name, node.FieldList.Fields.Select(f => new Field(new UnresolvedType(f.Declaration.Type.Name), f.Declaration.Name)));

                Symbols.Add(new TypeSymbol(node.Name, node.Source, struc));
            }

            public override void DefaultVisit(Node node)
            {
                foreach (var n in node.Children)
                {
                    n.Accept(this);
                }
            }
        }
    }
}

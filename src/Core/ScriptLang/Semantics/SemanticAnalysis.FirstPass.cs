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
            private readonly IUsingModuleResolver? usingResolver;

            public FirstPass(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols, IUsingModuleResolver? usingResolver)
                : base(diagnostics, filePath, symbols)
            {
                this.usingResolver = usingResolver;
            }

            protected override void OnEnd()
            {
                ResolveTypes();
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
                        var p = func.Parameters[i];
                        func.Parameters[i] = (Resolve(p.Type, source), p.Name);
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

            private FunctionType CreateUnresolvedFunctionType(Ast.Type? returnType, IEnumerable<VariableDeclaration> parameters)
            {
                var r = returnType != null ? UnresolvedTypeFromAst(returnType) : null;
                return new FunctionType(r, parameters.Select(p => ((Type)UnresolvedTypeFromAst(p.Type), (string?)p.Name)));
            }

            public override void VisitFunctionStatement(FunctionStatement node)
            {
                Symbols.Add(new FunctionSymbol(node, CreateUnresolvedFunctionType(node.ReturnType, node.ParameterList.Parameters)));
            }

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                Symbols.Add(new FunctionSymbol(node, CreateUnresolvedFunctionType(null, node.ParameterList.Parameters)));
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

            public override void VisitFunctionNativeStatement(FunctionNativeStatement node)
            {
                // TODO: check that native exists, if not report it in diagnostics
                Symbols.Add(new FunctionSymbol(node, CreateUnresolvedFunctionType(node.ReturnType, node.ParameterList.Parameters)));
            }

            public override void VisitProcedureNativeStatement(ProcedureNativeStatement node)
            {
                // TODO: check that native exists, if not report it in diagnostics
                Symbols.Add(new FunctionSymbol(node, CreateUnresolvedFunctionType(null, node.ParameterList.Parameters)));
            }

            public override void VisitStaticVariableStatement(StaticVariableStatement node)
            {
                Symbols.Add(new VariableSymbol(node.Variable.Declaration.Name,
                                               node.Source,
                                               UnresolvedTypeFromAst(node.Variable.Declaration.Type),
                                               VariableKind.Static));
            }

            public override void VisitStructStatement(StructStatement node)
            {
                var struc = new StructType(
                    node.Name,
                    node.FieldList.Fields.Select(f =>
                    {
                        if (f.Declaration.Type.IsReference)
                        {
                            Diagnostics.AddError(FilePath, $"Struct fields cannot be reference types", f.Declaration.Type.Source);
                        }

                        return new Field(UnresolvedTypeFromAst(f.Declaration.Type), f.Declaration.Name);
                    }
                ));

                Symbols.Add(new TypeSymbol(node.Name, node.Source, struc));
            }

            public override void VisitUsingStatement(UsingStatement node)
            {
                if (usingResolver == null)
                {
                    Diagnostics.AddError(FilePath, $"No USING resolver provided", node.Source);
                    return;
                }

                var importedModule = usingResolver.Resolve(node.Path);
                if (importedModule == null)
                {
                    Diagnostics.AddError(FilePath, $"Invalid USING path '{node.Path}'", node.Source);
                    return;
                }

                Debug.Assert(importedModule.State >= ModuleState.SemanticAnalysisFirstPassDone);
                Debug.Assert(importedModule.SymbolTable != null);

                Symbols.Import(importedModule.SymbolTable);
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

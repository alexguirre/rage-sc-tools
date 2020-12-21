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
                        case VariableSymbol s: s.Type = Resolve(s.Type, s.Source, ref anyUnresolved); break;
                        case FunctionSymbol s: ResolveFunc(s.Type, s.Source, ref anyUnresolved); break;
                        case TypeSymbol s when s.Type is StructType struc: ResolveStruct(struc, s.Source, ref anyUnresolved); break;
                        case TypeSymbol s when s.Type is FunctionType func: ResolveFunc(func, s.Source, ref anyUnresolved); break;
                    }
                }

                return !anyUnresolved;
            }

            private FunctionType CreateUnresolvedFunctionType(string? returnType, IEnumerable<VariableDeclaration> parameters)
            {
                var r = returnType != null ? TypeFromAst(returnType, null) : null;
                return new FunctionType(r, parameters.Select(p => (TypeFromAst(p.Type, p.Decl), (string?)p.Decl.Identifier)));
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
                Symbols.Add(new VariableSymbol(node.Variable.Declaration.Decl.Identifier,
                                               node.Source,
                                               TypeFromAst(node.Variable.Declaration.Type, node.Variable.Declaration.Decl),
                                               VariableKind.Static));
            }

            public override void VisitStructStatement(StructStatement node)
            {
                var struc = new StructType(
                    node.Name,
                    node.FieldList.Fields.Select(f =>
                    {
                        //if (f.Declaration.Type.IsReference)
                        //{
                        //    Diagnostics.AddError(FilePath, $"Struct fields cannot be reference types", f.Declaration.Source);
                        //}

                        return new Field(TypeFromAst(f.Declaration.Type, f.Declaration.Decl), f.Declaration.Decl.Identifier);
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

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
        /// Register global symbols (structs, static variables, constants, procedures and functions)
        /// </summary>
        private sealed class FirstPass : Pass
        {
            private readonly IUsingModuleResolver? usingResolver;
            private readonly Queue<(VariableSymbol Constant, Expression Initializer)> constantsToResolve = new();

            public FirstPass(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols, IUsingModuleResolver? usingResolver)
                : base(diagnostics, filePath, symbols)
            {
                this.usingResolver = usingResolver;
            }

            protected override void OnEnd()
            {
                ResolveConstants();
                ResolveTypes();
            }

            private void ResolveConstants()
            {
                var tmpDiagnostics = new DiagnosticsReport();
                var exprBinder = new ExpressionBinder(Symbols, tmpDiagnostics, FilePath);
                // TODO: exit gracefully in case of circular dependencies
                while (constantsToResolve.Count > 0)
                {
                    var c = constantsToResolve.Dequeue();
                    c.Constant.Initializer = exprBinder.Visit(c.Initializer)!;
                    if (c.Constant.Initializer.IsInvalid)
                    {
                        tmpDiagnostics.Clear();
                        constantsToResolve.Enqueue(c); // try again
                    }
                    else
                    {
                        // reduce the initializer to a literal
                        c.Constant.Initializer = ((BasicType)c.Constant.Initializer.Type!).TypeCode switch
                        {
                            BasicTypeCode.Bool => new Binding.BoundBoolLiteralExpression(Evaluator.Evaluate(c.Constant.Initializer)[0].AsUInt64 == 1),
                            BasicTypeCode.Int => new Binding.BoundIntLiteralExpression(Evaluator.Evaluate(c.Constant.Initializer)[0].AsInt32),
                            BasicTypeCode.Float => new Binding.BoundFloatLiteralExpression(Evaluator.Evaluate(c.Constant.Initializer)[0].AsFloat),
                            BasicTypeCode.String => c.Constant.Initializer, // if it is a STRING it should already be a literal
                            _ => throw new System.InvalidOperationException(),
                        };
                    }
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

            public override void VisitConstantVariableStatement(ConstantVariableStatement node)
            {
                bool unresolved = false;
                var ty = Resolve(TypeFromAst(node.Variable.Declaration.Type, node.Variable.Declaration.Decl), node.Source, ref unresolved);
                var error = unresolved || ty is not BasicType;
                if (error)
                {
                    Diagnostics.AddError(FilePath, $"The type '{ty}' cannot be CONST. Only INT, FLOAT, BOOL or STRING can be CONST.", node.Source);
                }

                var v = new VariableSymbol(node.Variable.Declaration.Decl.Identifier,
                                           node.Source,
                                           ty,
                                           VariableKind.Constant);
                Symbols.Add(v);
                if (node.Variable.Initializer != null && !error)
                {
                    constantsToResolve.Enqueue((v, node.Variable.Initializer));
                }
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

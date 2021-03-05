#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics.Binding;
    using ScTools.ScriptLang.Semantics.Symbols;

    public static partial class SemanticAnalysis
    {
        /// <summary>
        /// Register global symbols (structs, static variables, constants, procedures and functions)
        /// </summary>
        private sealed class FirstPass : Pass
        {
            private readonly IUsingModuleResolver? usingResolver;
            private readonly Queue<(VariableSymbol Constant, Expression Initializer, int NumUnresolved)> constantsToResolve = new();

            public FirstPass(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols, IUsingModuleResolver? usingResolver)
                : base(diagnostics, filePath, symbols)
            {
                this.usingResolver = usingResolver;
            }

            protected override void OnEnd()
            {
                ResolveConstants();
                ResolveTypes();
                CheckGlobalBlocks();
            }

            private void ResolveConstants()
            {
                var exprBinder = new ExpressionBinder(Symbols, new DiagnosticsReport(), FilePath);
                while (constantsToResolve.Count > 0)
                {
                    var c = constantsToResolve.Dequeue();
                    if (!IsExprConstant(c.Constant, c.Initializer))
                    {
                        continue;
                    }

                    var constantInitializer = exprBinder.Visit(c.Initializer)!;
                    if (constantInitializer.IsInvalid)
                    {
                        constantsToResolve.Enqueue(c); // try again
                    }
                    else
                    {
                        var numUnresolved = CountUnresolvedDependencies(constantInitializer);
                        if (numUnresolved == 0)
                        {
                            // reduce the initializer to a literal
                            c.Constant.Initializer = ((BasicType)constantInitializer.Type!).TypeCode switch
                            {
                                BasicTypeCode.Bool => new BoundBoolLiteralExpression(Evaluator.Evaluate(constantInitializer)[0].AsUInt64 == 1),
                                BasicTypeCode.Int => new BoundIntLiteralExpression(Evaluator.Evaluate(constantInitializer)[0].AsInt32),
                                BasicTypeCode.Float => new BoundFloatLiteralExpression(Evaluator.Evaluate(constantInitializer)[0].AsFloat),
                                BasicTypeCode.String => constantInitializer, // if it is a STRING it should already be a literal
                                _ => throw new System.InvalidOperationException(),
                            };
                        }
                        else
                        {
                            if (numUnresolved < c.NumUnresolved)
                            {
                                constantsToResolve.Enqueue((c.Constant, c.Initializer, numUnresolved)); // try again
                            }
                            else
                            {
                                Diagnostics.AddError(FilePath, $"The constant '{c.Constant.Name}' involves a circular definition", c.Initializer.Source);
                            }
                        }
                    }
                }

                bool IsExprConstant(VariableSymbol targetConstant, Expression expr)
                {
                    if (expr is IdentifierExpression idExpr)
                    {
                        switch (Symbols.Lookup(idExpr.Identifier))
                        {
                            case VariableSymbol v when !v.IsConstant:
                                Diagnostics.AddError(FilePath, $"The expression assigned to '{targetConstant.Name}' must be constant. The variable '{idExpr.Identifier}' is not constant", idExpr.Source);
                                return false;
                            case null:
                                Diagnostics.AddError(FilePath, $"Unknown symbol '{idExpr.Identifier}'", idExpr.Source);
                                return false;
                        }
                    }

                    return expr.Children.Where(c => c is Expression).All(e => IsExprConstant(targetConstant, (Expression)e));
                }

                static int CountUnresolvedDependencies(BoundExpression expr)
                {
                    int count = 0;

                    switch (expr)
                    {
                        case BoundAggregateExpression x: count += x.Expressions.Sum(CountUnresolvedDependencies); break;
                        case BoundUnaryExpression x: count += CountUnresolvedDependencies(x.Operand); break;
                        case BoundBinaryExpression x: count += CountUnresolvedDependencies(x.Left) + CountUnresolvedDependencies(x.Right);break;
                        case BoundVariableExpression x:
                            if (x.Var.Initializer == null)
                            {
                                count += 1;
                            }
                            break;
                    }

                    return count;
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
                        case FunctionSymbol s when s.Type is ExplicitFunctionType funcTy: ResolveFunc(funcTy, s.Source, ref anyUnresolved); break;
                        case TypeSymbol s when s.Type is StructType struc: ResolveStruct(struc, s.Source, ref anyUnresolved); break;
                        case TypeSymbol s when s.Type is ExplicitFunctionType func: ResolveFunc(func, s.Source, ref anyUnresolved); break;
                    }
                }

                return !anyUnresolved;
            }

            private void CheckGlobalBlocks()
            {
                var globalBlocksFromThisModule = Symbols.Symbols.OfType<GlobalBlock>();
                foreach (var globalBlock in globalBlocksFromThisModule)
                {
                    if (globalBlock.ExceedsMaxSize)
                    {
                        Diagnostics.AddError(
                            FilePath,
                            $"Global block {globalBlock.Block} (owner: {globalBlock.Owner}, size: 0x{globalBlock.Size:X}) exceeds maximum size (0x{GlobalBlock.MaxSize:X})",
                            globalBlock.Source);
                    }
                }

                var usedOwners = new HashSet<string>();
                var usedBlocks = new HashSet<int>();
                foreach (var globalBlock in globalBlocksFromThisModule.Concat(Symbols.Imports.SelectMany(i => i.Symbols.OfType<GlobalBlock>())))
                {
                    if (!usedOwners.Add(globalBlock.Owner))
                    {
                        Diagnostics.AddError(
                            FilePath, // TODO: the global block may have been defined in a different file from the one we are currently analyzing
                            $"Script '{globalBlock.Owner}' is owner of more than one global block",
                            globalBlock.Source);
                    }

                    if (!usedBlocks.Add(globalBlock.Block))
                    {
                        Diagnostics.AddError(
                            FilePath,
                            $"Global block {globalBlock.Block} is repeated",
                            globalBlock.Source);
                    }
                }
            }

            private ExplicitFunctionType CreateUnresolvedFunctionType(string? returnType, IEnumerable<Declaration> parameters)
            {
                var r = returnType != null ? new UnresolvedType(returnType) : null;
                return new ExplicitFunctionType(r, parameters.Select(p => (TypeFromDecl(p), (string?)p.Declarator.Identifier)));
            }

            public override void VisitFunctionStatement(FunctionStatement node)
            {
                Symbols.Add(new DefinedFunctionSymbol(node, CreateUnresolvedFunctionType(node.ReturnType, node.Parameters)));
            }

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                Symbols.Add(new DefinedFunctionSymbol(node, CreateUnresolvedFunctionType(null, node.Parameters)));
            }

            public override void VisitFunctionPrototypeStatement(FunctionPrototypeStatement node)
            {
                var func = CreateUnresolvedFunctionType(node.ReturnType, node.Parameters);

                Symbols.Add(new TypeSymbol(node.Name, node.Source, func));
            }

            public override void VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node)
            {
                var func = CreateUnresolvedFunctionType(null, node.Parameters);

                Symbols.Add(new TypeSymbol(node.Name, node.Source, func));
            }

            public override void VisitFunctionNativeStatement(FunctionNativeStatement node)
            {
                // TODO: check that native exists, if not report it in diagnostics
                Symbols.Add(new NativeFunctionSymbol(node, CreateUnresolvedFunctionType(node.ReturnType, node.Parameters)));
            }

            public override void VisitProcedureNativeStatement(ProcedureNativeStatement node)
            {
                // TODO: check that native exists, if not report it in diagnostics
                Symbols.Add(new NativeFunctionSymbol(node, CreateUnresolvedFunctionType(null, node.Parameters)));
            }

            public override void VisitStaticVariableStatement(StaticVariableStatement node)
            {
                Symbols.Add(new VariableSymbol(node.Declaration.Declarator.Identifier,
                                               node.Declaration.Source,
                                               TypeFromDecl(node.Declaration),
                                               VariableKind.Static));
            }

            public override void VisitConstantVariableStatement(ConstantVariableStatement node)
            {
                bool unresolved = false;
                var ty = Resolve(TypeFromDecl(node.Declaration), node.Source, ref unresolved);
                var error = unresolved || ty is not BasicType;
                if (error)
                {
                    Diagnostics.AddError(FilePath, $"The type '{ty}' cannot be CONST. Only INT, FLOAT, BOOL or STRING can be CONST.", node.Source);
                }

                var v = new VariableSymbol(node.Declaration.Declarator.Identifier,
                                           node.Source,
                                           ty,
                                           VariableKind.Constant);
                Symbols.Add(v);
                if (node.Declaration.Initializer != null && !error)
                {
                    constantsToResolve.Enqueue((v, node.Declaration.Initializer, int.MaxValue));
                }
            }

            public override void VisitGlobalBlockStatement(GlobalBlockStatement node)
            {
                var vars = new List<VariableSymbol>(node.Variables.Length);
                foreach (var decl in node.Variables)
                {
                    var v = new VariableSymbol(decl.Declarator.Identifier,
                                               decl.Source,
                                               TypeFromDecl(decl),
                                               VariableKind.Global);

                    Symbols.Add(v);
                    vars.Add(v);
                }
                Symbols.Add(new GlobalBlock(node.Block, node.Owner, vars, node.Source));
            }

            public override void VisitStructStatement(StructStatement node)
            {
                var struc = new StructType(node.Name,
                                           node.Fields.Select(decl =>
                                           {
                                               var ty = TypeFromDecl(decl);
                                               if (ty is RefType)
                                               {
                                                   Diagnostics.AddError(FilePath, $"Struct fields cannot be reference types", decl.Source);
                                               }

                                               return new Field(TypeFromDecl(decl), decl.Declarator.Identifier);
                                           }));

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

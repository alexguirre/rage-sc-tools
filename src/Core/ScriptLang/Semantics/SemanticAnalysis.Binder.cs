//#nullable enable
//namespace ScTools.ScriptLang.Semantics
//{
//    using System;
//    using System.Diagnostics;
//    using System.Linq;

//    using ScTools.ScriptLang.Ast;
//    using ScTools.ScriptLang.Semantics.Binding;
//    using ScTools.ScriptLang.Semantics.Symbols;

//    public static partial class SemanticAnalysis
//    {
//        private sealed class Binder : Pass
//        {
//            private readonly BoundModule module = new BoundModule();
//            private BoundFunction? func = null;

//            public Binder(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols)
//                : base(diagnostics, filePath, symbols)
//            { }

//            protected override void OnEnd()
//            {
//            }

//            public override void VisitFunctionStatement(FunctionStatement node)
//            {
//                var funcSymbol = Symbols.Lookup(node.Name) as FunctionSymbol;
//                Debug.Assert(funcSymbol != null);

//                VisitFunc(funcSymbol, node.ParameterList, node.Block);
//            }

//            public override void VisitProcedureStatement(ProcedureStatement node)
//            {
//                var funcSymbol = Symbols.Lookup(node.Name) as FunctionSymbol;
//                Debug.Assert(funcSymbol != null);

//                VisitFunc(funcSymbol, node.ParameterList, node.Block);
//            }

//            private void VisitFunc(FunctionSymbol func, ParameterList parameters, StatementBlock block)
//            {
//                this.func = new BoundFunction(func);
//                Symbols = Symbols.GetScope(block)!;
//                parameters.Accept(this);
//                block.Accept(this);
//                Symbols = Symbols.ExitScope();
//            }

//            public override void VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node)
//            {
//                // empty to avoid visiting its ParameterList
//            }

//            public override void VisitFunctionPrototypeStatement(FunctionPrototypeStatement node)
//            {
//                // empty to avoid visiting its ParameterList
//            }

//            public override void VisitParameterList(ParameterList node)
//            {
//                foreach (var p in node.Parameters)
//                {
//                    var v = new VariableSymbol(p.Name,
//                                               p.Source,
//                                               TryResolveType(p.Type.Name, p.Type.Source),
//                                               VariableKind.LocalArgument)
//                            {
//                                Location = funcAllocLocation,
//                            };
//                    int size = v.Type.SizeOf;
//                    funcAllocLocation += size;
//                    funcLocalArgsSize += size;
//                    Symbols.Add(v);
//                }
//                funcAllocLocation += 2; // space required by the game
//            }

//            public override void VisitVariableDeclarationStatement(VariableDeclarationStatement node)
//            {
//                var v = new VariableSymbol(node.Variable.Declaration.Name,
//                                           node.Source,
//                                           TryResolveType(node.Variable.Declaration.Type.Name, node.Variable.Declaration.Type.Source),
//                                           VariableKind.Local)
//                {
//                    Location = funcAllocLocation,
//                };

//                if (node.Variable.Initializer != null)
//                {
//                    var initializerType = TypeOf(node.Variable.Initializer);
//                    if (initializerType != null && initializerType != v.Type)
//                    {
//                        Diagnostics.AddError(FilePath, $"Mismatched initializer type and type of variable '{v.Name}'", node.Variable.Initializer.Source);
//                    }
//                }

//                int size = v.Type.SizeOf;
//                funcAllocLocation += size;
//                funcLocalsSize += size;
//                Symbols.Add(v);
//            }

//            public override void VisitAssignmentStatement(AssignmentStatement node)
//            {
//                var destType = TypeOf(node.Left);
//                var srcType = TypeOf(node.Right);
            
//                if (destType == null || srcType == null)
//                {
//                    return;
//                }

//                if (destType != srcType)
//                {
//                    Diagnostics.AddError(FilePath, "Mismatched types in assigment", node.Source);
//                }
//            }

//            public override void VisitIfStatement(IfStatement node)
//            {
//                var conditionType = TypeOf(node.Condition);
//                if (!(conditionType is BasicType { TypeCode: BasicTypeCode.Bool }))
//                {
//                    Diagnostics.AddError(FilePath, $"IF statement condition requires BOOL type", node.Condition.Source);
//                }

//                Symbols = Symbols.EnterScope(node.ThenBlock);
//                node.ThenBlock.Accept(this);
//                Symbols = Symbols.ExitScope();

//                if (node.ElseBlock != null)
//                {
//                    Symbols = Symbols.EnterScope(node.ElseBlock);
//                    node.ElseBlock.Accept(this);
//                    Symbols = Symbols.ExitScope();
//                }
//            }

//            public override void VisitWhileStatement(WhileStatement node)
//            {
//                var conditionType = TypeOf(node.Condition);
//                if (!(conditionType is BasicType { TypeCode: BasicTypeCode.Bool }))
//                {
//                    Diagnostics.AddError(FilePath, $"WHILE statement condition requires BOOL type", node.Condition.Source);
//                }

//                Symbols = Symbols.EnterScope(node.Block);
//                node.Block.Accept(this);
//                Symbols = Symbols.ExitScope();
//            }

//            public override void VisitReturnStatement(ReturnStatement node)
//            {
//                Debug.Assert(func != null);

//                if (node.Expression == null)
//                {
//                    // if this is a function, the missing expression should have been reported by SyntaxChecker
//                    return;
//                }

//                var returnType = TypeOf(node.Expression);
//                if (returnType != func.Type.ReturnType)
//                {
//                    Diagnostics.AddError(FilePath, $"Returned type does not match the specified function return type", node.Expression.Source);
//                }
//            }

//            public override void VisitInvocationStatement(InvocationStatement node)
//            {
//                // TODO: very similar to TypeOf.VisitInvocationExpression, refactor
//                var callableType = TypeOf(node.Expression);
//                if (!(callableType is FunctionType f))
//                {
//                    if (callableType != null)
//                    {
//                        Diagnostics.AddError(FilePath, $"Cannot call '{node.Expression}', it is not a procedure or a function", node.Expression.Source);
//                    }
//                    return;
//                }

//                int expected = f.Parameters.Count;
//                int found = node.ArgumentList.Arguments.Length;
//                if (found != expected)
//                {
//                    Diagnostics.AddError(FilePath, $"Mismatched number of arguments. Expected {expected}, found {found}", node.ArgumentList.Source);
//                }

//                int argCount = Math.Min(expected, found);
//                for (int i = 0; i < argCount; i++)
//                {
//                    var expectedType = f.Parameters[i];
//                    var foundType = TypeOf(node.ArgumentList.Arguments[i]);

//                    if (expectedType != foundType)
//                    {
//                        Diagnostics.AddError(FilePath, $"Mismatched type of argument #{i}", node.ArgumentList.Arguments[i].Source);
//                    }
//                }
//            }

//            public override void DefaultVisit(Node node)
//            {
//                foreach (var n in node.Children)
//                {
//                    n.Accept(this);
//                }
//            }
//        }
//    }
//}

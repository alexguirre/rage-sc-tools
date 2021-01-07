#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics.Binding;
    using ScTools.ScriptLang.Semantics.Symbols;

    public static partial class SemanticAnalysis
    {
        private sealed class Binder : Pass
        {
            public BoundModule Module { get; } = new BoundModule();
            private IList<BoundStatement>? stmts = null; // statements of the current block
            private readonly ExpressionBinder exprBinder;

            public Binder(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols)
                : base(diagnostics, filePath, symbols)
            {
                exprBinder = new ExpressionBinder(Symbols, Diagnostics, FilePath);
            }

            public override void VisitScriptNameStatement(ScriptNameStatement node) => Module.Name = node.Name;

            public override void VisitStaticVariableStatement(StaticVariableStatement node)
            {
                var s = Symbols.Lookup(node.Variable.Declaration.Decl.Identifier) as VariableSymbol;
                Debug.Assert(s != null);
                Debug.Assert(s.IsStatic);

                s.Initializer = Bind(node.Variable.Initializer);
                Module.Statics.Add(s);
            }

            public override void VisitFunctionStatement(FunctionStatement node) => VisitFunc(node.Name, node.Block);
            public override void VisitProcedureStatement(ProcedureStatement node) => VisitFunc(node.Name, node.Block);

            private void VisitFunc(string name, StatementBlock block)
            {
                var func = Symbols.Lookup(name) as FunctionSymbol;
                Debug.Assert(func != null);

                var boundFunc = new BoundFunction(func);
                Module.Functions.Add(boundFunc);
                VisitScope(block, boundFunc.Body);
            }

            public override void VisitAssignmentStatement(AssignmentStatement node)
            {
                stmts!.Add(new BoundAssignmentStatement(
                    Bind(node.Left)!,
                    Bind(node.Right)!
                ));
            }

            public override void VisitIfStatement(IfStatement node)
            {
                var boundIf = new BoundIfStatement(Bind(node.Condition)!);
                stmts!.Add(boundIf);

                VisitScope(node.ThenBlock, boundIf.Then);

                if (node.ElseBlock != null)
                {
                    VisitScope(node.ElseBlock, boundIf.Else);
                }
            }

            public override void VisitWhileStatement(WhileStatement node)
            {
                var boundWhile = new BoundWhileStatement(Bind(node.Condition)!);
                stmts!.Add(boundWhile);

                VisitScope(node.Block, boundWhile.Block);
            }

            public override void VisitSwitchStatement(SwitchStatement node)
            {
                var boundSwitch = new BoundSwitchStatement(Bind(node.Expression)!);
                stmts!.Add(boundSwitch);

                var handledCases = new HashSet<int?>();
                foreach (var c in node.Cases)
                {
                    int? value = null;
                    if (c is ValueSwitchCase v)
                    {
                        var boundExpr = Bind(v.Value)!;

                        if (!boundExpr.IsConstant)
                        {
                            Diagnostics.AddError(FilePath, $"Expected constant expression", v.Value.Source);
                            continue;
                        }
                        else if (boundExpr.Type is not BasicType { TypeCode: BasicTypeCode.Int })
                        {
                            continue;
                        }
                        else
                        {
                            value = Evaluator.Evaluate(boundExpr)[0].AsInt32;
                        }
                    }

                    if (!handledCases.Add(value))
                    {
                        Diagnostics.AddError(FilePath, value == null ? $"Default case already handled" : $"Case '{value.Value}' already handled", c.Source);
                    }

                    var boundCase = new BoundSwitchCase(value);
                    boundSwitch.Cases.Add(boundCase);
                    VisitScope(c.Block, boundCase.Block);
                }
            }

            public override void VisitReturnStatement(ReturnStatement node)
            {
                stmts!.Add(new BoundReturnStatement(
                    Bind(node.Expression)
                ));
            }

            public override void VisitInvocationStatement(InvocationStatement node)
            {
                stmts!.Add(new BoundInvocationStatement(
                    Bind(node.Expression)!,
                    node.ArgumentList.Arguments.Select(a => Bind(a)!)
                ));
            }

            public override void VisitVariableDeclarationStatement(VariableDeclarationStatement node) 
            {
                var varSymbol = Symbols.Lookup(node.Variable.Declaration.Decl.Identifier) as VariableSymbol;
                Debug.Assert(varSymbol != null);

                varSymbol.Initializer = Bind(node.Variable.Initializer);

                if (varSymbol.Type is RefType)
                {
                    var err = false;
                    if (varSymbol.Initializer is null)
                    {
                        Diagnostics.AddError(FilePath, $"Reference variable '{varSymbol.Name}' is missing an initializer", node.Source);
                        err = true;
                    }
                    else if (!varSymbol.Initializer.IsAddressable)
                    {
                        Diagnostics.AddError(FilePath, $"Cannot take reference of expression", node.Variable.Initializer!.Source);
                        err = true;
                    }

                    if (err)
                    {
                        stmts!.Add(new BoundInvalidStatement());
                        return;
                    }
                }

                stmts!.Add(new BoundVariableDeclarationStatement(varSymbol));
            }

            public override void VisitArgumentList(ArgumentList node) => throw new NotSupportedException();
            public override void VisitArrayIndexer(ArrayIndexer node) => throw new NotSupportedException();

            public override void VisitAggregateExpression(AggregateExpression node) => throw new NotSupportedException();
            public override void VisitArrayAccessExpression(ArrayAccessExpression node) => throw new NotSupportedException();
            public override void VisitBinaryExpression(BinaryExpression node) => throw new NotSupportedException();
            public override void VisitIdentifierExpression(IdentifierExpression node) => throw new NotSupportedException();
            public override void VisitInvocationExpression(InvocationExpression node) => throw new NotSupportedException();
            public override void VisitLiteralExpression(LiteralExpression node) => throw new NotSupportedException();
            public override void VisitMemberAccessExpression(MemberAccessExpression node) => throw new NotSupportedException();
            public override void VisitUnaryExpression(UnaryExpression node) => throw new NotSupportedException();

            public override void VisitRoot(Root node) => DefaultVisit(node);
            public override void VisitStatementBlock(StatementBlock node) => DefaultVisit(node);

            public override void VisitProcedurePrototypeStatement(ProcedurePrototypeStatement node) { /* empty */ }
            public override void VisitFunctionPrototypeStatement(FunctionPrototypeStatement node) { /* empty */ }
            public override void VisitProcedureNativeStatement(ProcedureNativeStatement node) { /* empty */ }
            public override void VisitFunctionNativeStatement(FunctionNativeStatement node) { /* empty */ }
            public override void VisitParameterList(ParameterList node) { /* empty */ }
            public override void VisitStructFieldList(StructFieldList node) { /* empty */ }
            public override void VisitStructStatement(StructStatement node) { /* empty */ }
            public override void VisitRefDeclarator(RefDeclarator node) { /* empty */ }
            public override void VisitSimpleDeclarator(SimpleDeclarator node) { /* empty */ }
            public override void VisitArrayDeclarator(ArrayDeclarator node) { /* empty */ }
            public override void VisitVariableDeclaration(VariableDeclaration node) { /* empty */ }
            public override void VisitVariableDeclarationWithInitializer(VariableDeclarationWithInitializer node) { /* empty */ }

            public override void DefaultVisit(Node node)
            {
                foreach (var n in node.Children)
                {
                    n.Accept(this);
                }
            }

            private void VisitScope(StatementBlock block, IList<BoundStatement> destStmts)
            {
                var prevStmts = stmts;
                stmts = destStmts;
                Symbols = Symbols.GetScope(block);
                VisitStatementBlock(block);
                Symbols = Symbols.ExitScope();
                stmts = prevStmts;
            }

            private BoundExpression? Bind(Expression? expr)
            {
                exprBinder.Symbols = Symbols;
                return expr?.Accept(exprBinder);
            }
        }

        public sealed class ExpressionBinder : AstVisitor<BoundExpression>
        {
            public SymbolTable? Symbols { get; set; }
            private DiagnosticsReport? Diagnostics { get; }
            private string? FilePath { get; }

            public ExpressionBinder()
                => (Symbols, Diagnostics, FilePath) = (null, null, null);

            public ExpressionBinder(SymbolTable symbols, DiagnosticsReport diagnostics, string filePath)
                => (Symbols, Diagnostics, FilePath) = (symbols, diagnostics, filePath);

            private BoundExpression? Bind(Expression? expr) => expr?.Accept(this);

            public override BoundExpression VisitUnaryExpression(UnaryExpression node)
                => new BoundUnaryExpression(
                    Bind(node.Operand)!,
                    node.Op
                );

            public override BoundExpression VisitBinaryExpression(BinaryExpression node)
                => new BoundBinaryExpression(
                    Bind(node.Left)!,
                    Bind(node.Right)!,
                    node.Op
                );

            public override BoundExpression VisitIdentifierExpression(IdentifierExpression node)
            {
                Debug.Assert(Symbols != null, "Found identifier but no SymbolTable provided");

                switch (Symbols.Lookup(node.Identifier))
                {
                    case FunctionSymbol fn: return new BoundFunctionExpression(fn);
                    case VariableSymbol v: return new BoundVariableExpression(v);
                    case null: // TODO: unresolved symbols?
                        Diagnostics?.AddError(FilePath!, $"Unknown symbol '{node.Identifier}'", node.Source);
                        return new BoundUnknownSymbolExpression(node.Identifier);
                    default: throw new NotSupportedException();
                };
            }
            public override BoundExpression VisitInvocationExpression(InvocationExpression node)
            {
                var callee = Bind(node.Expression)!;

                if (callee.Type is not FunctionType funcTy || funcTy.ReturnType == null)
                {
                    return new BoundInvalidExpression("Callee is a procedure in an invocation expression");
                }

                return new BoundInvocationExpression(
                    callee,
                    node.ArgumentList.Arguments.Select(a => Bind(a)!)
                );
            }

            public override BoundExpression VisitMemberAccessExpression(MemberAccessExpression node)
            {
                var expr = Bind(node.Expression)!;

                if (expr is not BoundInvalidExpression && !expr.Type!.UnderlyingType.HasField(node.Member))
                {
                    return new BoundUnknownMemberAccessExpression(expr, node.Member);
                }

                return new BoundMemberAccessExpression(expr, node.Member);
            }

            public override BoundExpression VisitArrayAccessExpression(ArrayAccessExpression node)
                => new BoundArrayAccessExpression(
                    Bind(node.Expression)!,
                    Bind(node.Indexer.Expression)!
                );

            public override BoundExpression VisitAggregateExpression(AggregateExpression node)
                => new BoundAggregateExpression(
                    node.Expressions.Select(Bind)!
                );

            public override BoundExpression VisitLiteralExpression(LiteralExpression node)
                => node.Kind switch
                {
                    LiteralKind.Int => new BoundIntLiteralExpression(node.IntValue),
                    LiteralKind.Float => new BoundFloatLiteralExpression(node.FloatValue),
                    LiteralKind.String => new BoundStringLiteralExpression(node.StringValue),
                    LiteralKind.Bool => new BoundBoolLiteralExpression(node.BoolValue),
                    _ => throw new NotSupportedException(),
                };

            public override BoundExpression VisitErrorExpression(ErrorExpression node) => new BoundInvalidExpression($"{nameof(ErrorExpression)}: '{node.Text}'");

            public override BoundExpression DefaultVisit(Node node) => throw new InvalidOperationException($"Unsupported AST node {node.GetType().Name}");
        }
    }
}

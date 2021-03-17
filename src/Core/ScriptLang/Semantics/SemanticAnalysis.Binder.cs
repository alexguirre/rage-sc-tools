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

            public Binder(DiagnosticsReport diagnostics, SymbolTable symbols)
                : base(diagnostics, symbols)
            {
                exprBinder = new ExpressionBinder(Symbols, Diagnostics);
            }

            public override void VisitScriptNameStatement(ScriptNameStatement node) => Module.Name = node.Name;
            public override void VisitScriptHashStatement(ScriptHashStatement node) => Module.Hash = node.Hash;

            public override void VisitStaticVariableStatement(StaticVariableStatement node)
            {
                var s = Symbols.Lookup(node.Declaration.Declarator.Identifier) as VariableSymbol;
                Debug.Assert(s != null);
                Debug.Assert(s.IsStatic);

                s.Initializer = Bind(node.Declaration.Initializer);
                Module.Statics.Add(s);
            }

            public override void VisitGlobalBlockStatement(GlobalBlockStatement node)
            {
                foreach (var v in node.Variables)
                {
                    var s = Symbols.Lookup(v.Declarator.Identifier) as VariableSymbol;
                    Debug.Assert(s != null);
                    Debug.Assert(s.IsGlobal);

                    s.Initializer = Bind(v.Initializer);
                }
            }

            public override void VisitFunctionStatement(FunctionStatement node) => VisitFunc(node.Name, node.Block);
            public override void VisitProcedureStatement(ProcedureStatement node) => VisitFunc(node.Name, node.Block);

            private void VisitFunc(string name, StatementBlock block)
            {
                var func = Symbols.Lookup(name) as DefinedFunctionSymbol;
                Debug.Assert(func != null);

                var boundFunc = new BoundFunction(func);
                Module.Functions.Add(boundFunc);
                VisitScope(block, boundFunc.Body);
            }

            public override void VisitAssignmentStatement(AssignmentStatement node)
            {
                var left = Bind(node.Left)!;
                var right = Bind(node.Right)!;

                if (node.Op != null)
                {
                    // transform 'left op= right' to 'left = left op right'
                    // TODO: this may cause 'left' to be evaluated twice (e.g. function call that returns a reference)
                    right = new BoundBinaryExpression(left, right, node.Op.Value);
                }

                stmts!.Add(new BoundAssignmentStatement(left, right));
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

            public override void VisitRepeatStatement(RepeatStatement node)
            {
                // reduce REPEAT statement to a WHILE statement
                /* REPEAT n i
                 *    block
                 * ENDREPEAT
                 *
                 * i = 0
                 * WHILE i < n
                 *    block
                 *    i = i + 1
                 * ENDWHILE
                 */

                var boundLimit = Bind(node.Limit)!;
                var boundCounter = Bind(node.Counter)!;

                // i = 0
                var zeroCounter = new BoundAssignmentStatement(boundCounter, new BoundIntLiteralExpression(0));
                stmts!.Add(zeroCounter);

                // WHILE i < n
                var condition = new BoundBinaryExpression(boundCounter, boundLimit, BinaryOperator.Less);
                var boundWhile = new BoundWhileStatement(condition);
                stmts!.Add(boundWhile);

                VisitScope(node.Block, boundWhile.Block);

                // i = i + 1
                var add = new BoundBinaryExpression(boundCounter, new BoundIntLiteralExpression(1), BinaryOperator.Add);
                var assign = new BoundAssignmentStatement(boundCounter, add);
                boundWhile.Block.Add(assign);
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
                            Diagnostics.AddError($"Expected constant expression", v.Value.Source);
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
                        Diagnostics.AddError(value == null ? $"Default case already handled" : $"Case '{value.Value}' already handled", c.Source);
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
                    node.Arguments.Select(a => Bind(a)!)
                ));
            }

            public override void VisitVariableDeclarationStatement(VariableDeclarationStatement node)
            {
                var varSymbol = Symbols.Lookup(node.Declaration.Declarator.Identifier) as VariableSymbol;
                Debug.Assert(varSymbol != null);

                varSymbol.Initializer = Bind(node.Declaration.Initializer);

                if (varSymbol.Type is RefType)
                {
                    var err = false;
                    if (varSymbol.Initializer is null)
                    {
                        Diagnostics.AddError($"Reference variable '{varSymbol.Name}' is missing an initializer", node.Source);
                        err = true;
                    }
                    else if (!varSymbol.Initializer.IsAddressable)
                    {
                        Diagnostics.AddError($"Cannot take reference of expression", node.Declaration.Initializer!.Source);
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

            public override void VisitVectorExpression(VectorExpression node) => throw new NotSupportedException();
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
            public override void VisitStructStatement(StructStatement node) { /* empty */ }
            public override void VisitRefDeclarator(RefDeclarator node) { /* empty */ }
            public override void VisitSimpleDeclarator(SimpleDeclarator node) { /* empty */ }
            public override void VisitArrayDeclarator(ArrayDeclarator node) { /* empty */ }
            public override void VisitDeclaration(Declaration node) { /* empty */ }

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

            public ExpressionBinder()
                => (Symbols, Diagnostics) = (null, null);

            public ExpressionBinder(SymbolTable symbols, DiagnosticsReport diagnostics)
                => (Symbols, Diagnostics) = (symbols, diagnostics);

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
                        Diagnostics?.AddError($"Unknown symbol '{node.Identifier}'", node.Source);
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
                    node.Arguments.Select(a => Bind(a)!)
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
                    Bind(node.Index)!
                );

            public override BoundExpression VisitVectorExpression(VectorExpression node)
                => new BoundVectorExpression(Bind(node.X)!, Bind(node.Y)!, Bind(node.Z)!);

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

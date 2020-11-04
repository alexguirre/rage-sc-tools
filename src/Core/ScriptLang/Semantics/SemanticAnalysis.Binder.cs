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

            public Binder(DiagnosticsReport diagnostics, string filePath, SymbolTable symbols, int staticVarsTotalSize)
                : base(diagnostics, filePath, symbols)
            {
                Module.StaticVarsTotalSize = staticVarsTotalSize;
            }

            protected override void OnEnd()
            {
            }

            public override void VisitScriptNameStatement(ScriptNameStatement node)
            {
                Module.Name = node.Name;
            }

            public override void VisitFunctionStatement(FunctionStatement node)
            {
                var funcSymbol = Symbols.Lookup(node.Name) as FunctionSymbol;
                Debug.Assert(funcSymbol != null);

                VisitFunc(funcSymbol, node.Block);
            }

            public override void VisitProcedureStatement(ProcedureStatement node)
            {
                var funcSymbol = Symbols.Lookup(node.Name) as FunctionSymbol;
                Debug.Assert(funcSymbol != null);

                VisitFunc(funcSymbol, node.Block);
            }

            private void VisitFunc(FunctionSymbol func, StatementBlock block)
            {
                var boundFunc = new BoundFunction(func);
                Module.Functions.Add(boundFunc);
                stmts = boundFunc.Body;
                Symbols = Symbols.GetScope(block)!;
                block.Accept(this);
                Symbols = Symbols.ExitScope();
                stmts = null;
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

                var prevBlock = stmts;

                stmts = boundIf.Then;
                VisitStatementBlock(node.ThenBlock);

                if (node.ElseBlock != null)
                {
                    stmts = boundIf.Else;
                    VisitStatementBlock(node.ElseBlock);
                }

                stmts = prevBlock;
            }

            public override void VisitWhileStatement(WhileStatement node)
            {
                var boundWhile = new BoundWhileStatement(Bind(node.Condition)!);
                stmts!.Add(boundWhile);

                var prevBlock = stmts;

                stmts = boundWhile.Block;
                VisitStatementBlock(node.Block);

                stmts = prevBlock;
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
                var varSymbol = Symbols.Lookup(node.Variable.Declaration.Name) as VariableSymbol;
                Debug.Assert(varSymbol != null);

                stmts!.Add(new BoundVariableDeclarationStatement(
                    varSymbol,
                    Bind(node.Variable.Initializer)
                ));
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
            public override void VisitStaticVariableStatement(StaticVariableStatement node) { /* empty */ }
            public override void VisitStructFieldList(StructFieldList node) { /* empty */ }
            public override void VisitStructStatement(StructStatement node) { /* empty */ }
            public override void VisitType(Ast.Type node) { /* empty */ }
            public override void VisitVariableDeclaration(VariableDeclaration node) { /* empty */ }
            public override void VisitVariableDeclarationWithInitializer(VariableDeclarationWithInitializer node) { /* empty */ }

            public override void DefaultVisit(Node node)
            {
                foreach (var n in node.Children)
                {
                    n.Accept(this);
                }
            }

            private BoundExpression? Bind(Expression? expr) => expr?.Accept(new ExpressionBinder(Symbols));


            private sealed class ExpressionBinder : AstVisitor<BoundExpression>
            {
                private SymbolTable Symbols { get; }

                public ExpressionBinder(SymbolTable symbols) => Symbols = symbols;

                private BoundExpression? Bind(Expression? expr) => expr?.Accept(new ExpressionBinder(Symbols));

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
                    var symbol = Symbols.Lookup(node.Identifier);
                    if (symbol is FunctionSymbol fn)
                    {
                        return new BoundFunctionExpression(fn);
                    }
                    else if (symbol is VariableSymbol v)
                    {
                        return new BoundVariableExpression(v);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }

                public override BoundExpression VisitInvocationExpression(InvocationExpression node)
                    => new BoundInvocationExpression(
                        Bind(node.Expression)!,
                        node.ArgumentList.Arguments.Select(a => Bind(a)!)
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

                public override BoundExpression VisitAggregateExpression(AggregateExpression node) => throw new NotImplementedException();
                public override BoundExpression VisitArrayAccessExpression(ArrayAccessExpression node) => throw new NotImplementedException();
                public override BoundExpression VisitMemberAccessExpression(MemberAccessExpression node) => throw new NotImplementedException();
            }
        }
    }
}

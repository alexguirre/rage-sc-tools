namespace ScTools.Tests.ScriptLang
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;

    using Xunit;
    using static Xunit.Assert;

    public partial class ParserTests
    {
        [Fact]
        public void Label()
        {
            var p = ParserFor(
                "my_label:"
            );

            True(p.IsPossibleLabel());
            Assert(p.ParseLabel(), n => n is Label { Name: "my_label" });
            NoErrorsAndIsAtEOF(p);

        }

        private static void NoErrorsAndIsAtEOF(ParserNew p)
        {
            False(p.Diagnostics.HasErrors);
            False(p.Diagnostics.HasWarnings);
            True(p.IsAtEOF);
        }
        private static void Assert(INode? node, Predicate<INode?> predicate)
        {
            False(node is IError);
            True(predicate(node));
        }
        private static void AssertParseExpression(ParserNew p, Predicate<IExpression> predicate)
        {
            True(p.IsPossibleExpression());
            var expr = p.ParseExpression();
            False(expr is IError);
            True(predicate(expr));
        }
        private static void AssertInvocation(IStatement stmt, Predicate<IExpression> calleePredicate, Action<ImmutableArray<IExpression>> argumentsChecker)
        {
            True(stmt is InvocationExpression);
            if (stmt is InvocationExpression invocationExpr)
            {
                True(calleePredicate(invocationExpr.Callee));
                argumentsChecker(invocationExpr.Arguments);
            }
        }
        private static void AssertInvocation(IExpression expr, Predicate<IExpression> calleePredicate, Action<ImmutableArray<IExpression>> argumentsChecker)
        {
            True(expr is InvocationExpression);
            if (expr is InvocationExpression invocationExpr)
            {
                True(calleePredicate(invocationExpr.Callee));
                argumentsChecker(invocationExpr.Arguments);
            }
        }
        private static void AssertIfStmt(IStatement stmt, Predicate<IExpression> conditionPredicate, Action<ImmutableArray<IStatement>> thenBodyChecker, Action<ImmutableArray<IStatement>> elseBodyChecker)
        {
            True(stmt is IfStatement);
            if (stmt is IfStatement ifStmt)
            {
                True(conditionPredicate(ifStmt.Condition));
                thenBodyChecker(ifStmt.Then);
                elseBodyChecker(ifStmt.Else);
            }
        }
        private static void AssertWhileStmt(IStatement stmt, Predicate<IExpression> conditionPredicate, Action<ImmutableArray<IStatement>> bodyChecker)
        {
            True(stmt is WhileStatement);
            if (stmt is WhileStatement whileStmt)
            {
                True(conditionPredicate(whileStmt.Condition));
                bodyChecker(whileStmt.Body);
            }
        }
        private static void AssertRepeatStmt(IStatement stmt, Predicate<IExpression> limitPredicate, Predicate<IExpression> counterPredicate, Action<ImmutableArray<IStatement>> bodyChecker)
        {
            True(stmt is RepeatStatement);
            if (stmt is RepeatStatement repeatStmt)
            {
                True(limitPredicate(repeatStmt.Limit));
                True(counterPredicate(repeatStmt.Counter));
                bodyChecker(repeatStmt.Body);
            }
        }
        private static void AssertSwitchStmt(IStatement stmt, Predicate<IExpression> expressionPredicate, Action<ImmutableArray<SwitchCase>> casesChecker)
        {
            True(stmt is SwitchStatement);
            if (stmt is SwitchStatement switchStmt)
            {
                True(expressionPredicate(switchStmt.Expression));
                casesChecker(switchStmt.Cases);
            }
        }
        private static void AssertSwitchCase(SwitchCase switchCase, Predicate<SwitchCase> switchCasePredicate, Action<ImmutableArray<IStatement>> bodyChecker)
        {
            True(switchCasePredicate(switchCase));
            bodyChecker(switchCase.Body);
        }
        private static void AssertArrayVarDeclaration(IStatement stmt, Predicate<VarDeclaration_New> varDeclPredicate, params Predicate<IExpression?>[] expectedLengths)
        {
            True(stmt is VarDeclaration_New);
            if (stmt is VarDeclaration_New varDecl)
            {
                True(varDecl.Declarator is VarArrayDeclarator);
                True(varDeclPredicate(varDecl));
                Equal(expectedLengths.Length, ((VarArrayDeclarator)varDecl.Declarator).Rank);
                int i = 0;
                foreach(var length in ((VarArrayDeclarator)varDecl.Declarator).Lengths)
                {
                    True(expectedLengths[i](length));
                    i++;
                }
            }
        }
        private static void AssertError(INode node, Predicate<INode> predicate)
        {
            True(node is IError);
            True(predicate(node));
        }

        private static void CheckError(ErrorCode expectedError, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, DiagnosticsReport diagnostics)
        {
            var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
            Equal(1, diagnostics.Errors.Count(err => err.Code == (int)expectedError && err.Source == expectedLocation));
        }

        private const string TestFileName = "parser_tests.sc";

        private static SourceRange MakeSourceRange((int Line, int Column) start, (int Line, int Column) end)
            => new((start.Line, start.Column, TestFileName), (end.Line, end.Column, TestFileName));

        private static ParserNew ParserFor(string source)
        {
            var lexer = new Lexer(TestFileName, source, new DiagnosticsReport());
            return new(lexer, lexer.Diagnostics);
        }
    }
}

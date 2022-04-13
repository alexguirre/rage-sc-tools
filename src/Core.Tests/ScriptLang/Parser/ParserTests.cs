namespace ScTools.Tests.ScriptLang
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast;
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
            var label = p.ParseLabel();
            True(p.IsAtEOF);

            Equal("my_label", label);
        }

        private static void Assert(INode node, Predicate<INode> predicate)
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

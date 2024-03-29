﻿namespace ScTools.Tests.ScriptLang.Parser;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Errors;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;

public partial class ParserTests
{
    private static void NoErrorsAndIsAtEOF(Parser p)
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
    private static void ParseExpressionAndAssert(Parser p, Predicate<IExpression> predicate)
    {
        True(p.IsPossibleExpression());
        var expr = p.ParseExpression();
        False(expr is IError);
        True(predicate(expr));
    }
    private static void AssertInvocationStmt(IStatement stmt, Predicate<IExpression> calleePredicate, Action<ImmutableArray<IExpression>> argumentsChecker)
    {
        True(stmt is ExpressionStatement);
        if (stmt is ExpressionStatement exprStmt)
        {
            AssertInvocation(exprStmt.Expression, calleePredicate, argumentsChecker);
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
    private static void AssertForStmt(IStatement stmt, Predicate<IExpression> counterPredicate, Predicate<IExpression> initializerPredicate, Predicate<IExpression> limitPredicate, Action<ImmutableArray<IStatement>> bodyChecker)
    {
        True(stmt is ForStatement);
        if (stmt is ForStatement forStmt)
        {
            True(counterPredicate(forStmt.Counter));
            True(initializerPredicate(forStmt.Initializer));
            True(limitPredicate(forStmt.Limit));
            bodyChecker(forStmt.Body);
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
    private static void AssertArrayVarDeclaration(IStatement stmt, Predicate<VarDeclaration> varDeclPredicate, params Predicate<IExpression?>[] expectedLengths)
    {
        True(stmt is VarDeclaration);
        if (stmt is VarDeclaration varDecl)
        {
            True(varDecl.Declarator.IsArray);
            True(varDeclPredicate(varDecl));
            Equal(expectedLengths.Length, varDecl.Declarator.Rank);
            int i = 0;
            foreach(var length in varDecl.Declarator.Lengths)
            {
                True(expectedLengths[i](length));
                i++;
            }
        }
    }
    private static void AssertEnumDeclaration(INode node, string name, Action<ImmutableArray<EnumMemberDeclaration>> membersChecker)
    {
        True(node is EnumDeclaration);
        if (node is EnumDeclaration enumDecl)
        {
            Equal(name, enumDecl.Name);
            membersChecker(enumDecl.Members);
        }
    }
    private static void AssertStructDeclaration(INode node, string name, Action<ImmutableArray<VarDeclaration>> fieldsChecker)
    {
        True(node is StructDeclaration);
        if (node is StructDeclaration structDecl)
        {
            Equal(name, structDecl.Name);
            fieldsChecker(structDecl.Fields);
        }
    }
    private static void AssertGlobalBlockDeclaration(INode node, string name, int blockIndex, Action<ImmutableArray<VarDeclaration>> varsChecker)
    {
        True(node is GlobalBlockDeclaration);
        if (node is GlobalBlockDeclaration globalBlockDecl)
        {
            Equal(name, globalBlockDecl.Name);
            Equal(blockIndex, globalBlockDecl.BlockIndex);
            varsChecker(globalBlockDecl.Vars);
        }
    }
    private static void AssertScriptDeclaration(INode node, string name, Action<ImmutableArray<VarDeclaration>> parametersChecker, Action<ImmutableArray<IStatement>> bodyChecker)
    {
        True(node is ScriptDeclaration);
        if (node is ScriptDeclaration scriptDecl)
        {
            Equal(name, scriptDecl.Name);
            parametersChecker(scriptDecl.Parameters);
            bodyChecker(scriptDecl.Body);
        }
    }
    private static void AssertFunctionDeclaration(INode node, string name, Predicate<TypeName?> returnTypePredicate, Action<ImmutableArray<VarDeclaration>> parametersChecker, Action<ImmutableArray<IStatement>> bodyChecker, bool isDebugOnly)
    {
        True(node is FunctionDeclaration);
        if (node is FunctionDeclaration funcDecl)
        {
            Equal(name, funcDecl.Name);
            True(returnTypePredicate(funcDecl.ReturnType));
            parametersChecker(funcDecl.Parameters);
            bodyChecker(funcDecl.Body);
            Equal(isDebugOnly, funcDecl.IsDebugOnly);
        }
    }
    private static void AssertFunctionTypeDefDeclaration(INode node, string name, Predicate<TypeName?> returnTypePredicate, Action<ImmutableArray<VarDeclaration>> parametersChecker)
    {
        True(node is FunctionTypeDefDeclaration);
        if (node is FunctionTypeDefDeclaration funcPtrDecl)
        {
            Equal(name, funcPtrDecl.Name);
            True(returnTypePredicate(funcPtrDecl.ReturnType));
            parametersChecker(funcPtrDecl.Parameters);
        }
    }
    private static void AssertNativeFunctionDeclaration(INode node, string name, Predicate<TypeName?> returnTypePredicate, Action<ImmutableArray<VarDeclaration>> parametersChecker, Predicate<IExpression?> idPredicate, bool isDebugOnly)
    {
        True(node is NativeFunctionDeclaration);
        if (node is NativeFunctionDeclaration funcDecl)
        {
            Equal(name, funcDecl.Name);
            True(returnTypePredicate(funcDecl.ReturnType));
            parametersChecker(funcDecl.Parameters);
            True(idPredicate(funcDecl.Id));
            Equal(isDebugOnly, funcDecl.IsDebugOnly);
        }
    }
    private static void AssertNativeTypeDeclaration(INode node, string name)
    {
        True(node is NativeTypeDeclaration);
        if (node is NativeTypeDeclaration typeDecl)
        {
            Equal(name, typeDecl.Name);
        }
    }
    private static void AssertError(INode node, Predicate<INode> predicate)
    {
        True(node is IError);
        True(predicate(node));
    }

    private static void CheckError(ErrorCode expectedError, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, DiagnosticsReport diagnostics, int expectedNumMatchingErrors = 1)
    {
        var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
        Equal(expectedNumMatchingErrors, diagnostics.Errors.Count(err => err.Code == (int)expectedError && err.Source == expectedLocation));
    }

    private const string TestFileName = "parser_tests.sc";

    private static SourceRange MakeSourceRange((int Line, int Column) start, (int Line, int Column) end)
        => new((start.Line, start.Column, TestFileName), (end.Line, end.Column, TestFileName));

    private static Parser ParserFor(string source)
    {
        var lexer = new Lexer(TestFileName, source, new DiagnosticsReport());
        return new(lexer, lexer.Diagnostics, new(lexer.Diagnostics));
    }
}

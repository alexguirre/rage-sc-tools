namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System;

using Xunit;

using static Xunit.Assert;

public class InvocationTests : SemanticsTestsBase
{
    [Fact]
    public void CanCallFunctionWithoutParamsAsExpression()
    {
        var (s, ast) = AnalyzeAndAst(
            @"FUNC INT foo()
                RETURN 123
              ENDFUNC

              SCRIPT test
                IF foo()
                ENDIF
              ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
        var stmt = (IfStatement)((ScriptDeclaration)ast.Declarations[1]).Body[0];
        True(stmt.Condition is InvocationExpression { Semantics: { Type: IntType, ValueKind: ValueKind.RValue } });
    }

    [Theory]
    [InlineData("FUNC INT foo()\nRETURN 123\nENDFUNC", typeof(IntType))]
    [InlineData("PROC foo()\nENDPROC", typeof(VoidType))]
    public void CanCallFunctionWithoutParamsAsStatement(string funcDecl, Type type)
    {
        var (s, ast) = AnalyzeAndAst(
            @$"{funcDecl}

              SCRIPT test
                foo()
              ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
        var stmt = ((ScriptDeclaration)ast.Declarations[1]).Body[0];
        True(stmt is InvocationExpression { Semantics.ValueKind: ValueKind.RValue } inv
            && inv.Semantics.Type == (TypeInfo)Activator.CreateInstance(type)!);
    }

    [Fact]
    public void CanCallFunctionWithParamsAsExpression()
    {
        var (s, ast) = AnalyzeAndAst(
            @"FUNC INT foo(INT n, BOOL b)
                RETURN n
              ENDFUNC

              SCRIPT test
                IF foo(123, TRUE)
                ENDIF
              ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
        var stmt = (IfStatement)((ScriptDeclaration)ast.Declarations[1]).Body[0];
        True(stmt.Condition is InvocationExpression { Semantics: { Type: IntType, ValueKind: ValueKind.RValue } });
    }

    [Theory]
    [InlineData("FUNC INT foo(INT n, BOOL b)\nRETURN n\nENDFUNC", typeof(IntType))]
    [InlineData("PROC foo(INT n, BOOL b)\nENDPROC", typeof(VoidType))]
    public void CanCallFunctionWithParamsAsStatement(string funcDecl, Type type)
    {
        var (s, ast) = AnalyzeAndAst(
            @$"{funcDecl}

              SCRIPT test
                foo(123, TRUE)
              ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
        var stmt = ((ScriptDeclaration)ast.Declarations[1]).Body[0];
        True(stmt is InvocationExpression { Semantics.ValueKind: ValueKind.RValue } inv
            && inv.Semantics.Type == (TypeInfo)Activator.CreateInstance(type)!);
    }

    [Fact]
    public void FunctionCallsAsParametersWork()
    {
        var (s, ast) = AnalyzeAndAst(
            @$"FUNC INT foo(INT n, INT m)
                RETURN n + m
              ENDFUNC

              SCRIPT test
                foo(foo(1, foo(2, 3)), foo(foo(4, 5), 6))
              ENDSCRIPT"
        );

        False(s.Diagnostics.HasErrors);
        var stmt = ((ScriptDeclaration)ast.Declarations[1]).Body[0];
        True(stmt is InvocationExpression { Semantics: { Type: IntType, ValueKind: ValueKind.RValue } });
    }
}

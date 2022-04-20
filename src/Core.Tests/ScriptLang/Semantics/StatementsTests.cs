namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System;

using Xunit;

using static Xunit.Assert;

public class StatementsTests : SemanticsTestsBase
{
    [Fact]
    public void BoolLiteralWorksOnWhileLoops()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo()
                WHILE TRUE
                ENDWHILE
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var whileStmt = (WhileStatement)((FunctionDeclaration)ast.Declarations[0]).Body[0];
        True(whileStmt.Condition.Semantics is { Type: BoolType, ValueKind: ValueKind.RValue | ValueKind.Constant });
    }

    [Fact]
    public void IntLiteralWorksOnWhileLoops()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo()
                WHILE 1
                ENDWHILE
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var whileStmt = (WhileStatement)((FunctionDeclaration)ast.Declarations[0]).Body[0];
        True(whileStmt.Condition.Semantics is { Type: IntType, ValueKind: ValueKind.RValue | ValueKind.Constant });
    }

    [Fact]
    public void ParameterWorksOnWhileLoops()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo(BOOL b)
                WHILE b
                ENDWHILE
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var whileStmt = (WhileStatement)((FunctionDeclaration)ast.Declarations[0]).Body[0];
        True(whileStmt.Condition.Semantics is { Type: BoolType, ValueKind: ValueKind.RValue | ValueKind.Assignable | ValueKind.Addressable });
    }

    [Fact]
    public void StaticVariableWorksOnWhileLoops()
    {
        var (s, ast) = AnalyzeAndAst(
            @"BOOL b
              PROC foo()
                WHILE b
                ENDWHILE
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var whileStmt = (WhileStatement)((FunctionDeclaration)ast.Declarations[1]).Body[0];
        True(whileStmt.Condition.Semantics is { Type: BoolType, ValueKind: ValueKind.RValue | ValueKind.Assignable | ValueKind.Addressable });
    }

    [Fact]
    public void LocalVariableWorksOnWhileLoops()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo()
                BOOL b
                WHILE b
                ENDWHILE
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var whileStmt = (WhileStatement)((FunctionDeclaration)ast.Declarations[0]).Body[1];
        True(whileStmt.Condition.Semantics is { Type: BoolType, ValueKind: ValueKind.RValue | ValueKind.Assignable | ValueKind.Addressable });
    }

    [Fact]
    public void BoolLiteralWorksOnIfStatements()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo()
                IF TRUE
                ENDIF
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var ifStmt = (IfStatement)((FunctionDeclaration)ast.Declarations[0]).Body[0];
        True(ifStmt.Condition.Semantics is { Type: BoolType, ValueKind: ValueKind.RValue | ValueKind.Constant });
    }

    [Fact]
    public void IntLiteralWorksOnIfStatements()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo()
                IF 1
                ENDIF
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var ifStmt = (IfStatement)((FunctionDeclaration)ast.Declarations[0]).Body[0];
        True(ifStmt.Condition.Semantics is { Type: IntType, ValueKind: ValueKind.RValue | ValueKind.Constant });
    }

    [Fact]
    public void ParameterWorksOnIfStatements()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo(BOOL b)
                IF b
                ENDIF
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var ifStmt = (IfStatement)((FunctionDeclaration)ast.Declarations[0]).Body[0];
        True(ifStmt.Condition.Semantics is { Type: BoolType, ValueKind: ValueKind.RValue | ValueKind.Assignable | ValueKind.Addressable });
    }

    [Fact]
    public void StaticVariableWorksOnIfStatements()
    {
        var (s, ast) = AnalyzeAndAst(
            @"BOOL b
              PROC foo()
                IF b
                ENDIF
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var ifStmt = (IfStatement)((FunctionDeclaration)ast.Declarations[1]).Body[0];
        True(ifStmt.Condition.Semantics is { Type: BoolType, ValueKind: ValueKind.RValue | ValueKind.Assignable | ValueKind.Addressable });
    }

    [Fact]
    public void LocalVariableWorksOnIfStatements()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo()
                BOOL b
                IF b
                ENDIF
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var ifStmt = (IfStatement)((FunctionDeclaration)ast.Declarations[0]).Body[1];
        True(ifStmt.Condition.Semantics is { Type: BoolType, ValueKind: ValueKind.RValue | ValueKind.Assignable | ValueKind.Addressable });
    }

    [Theory]
    [InlineData("STRING")]
    [InlineData("FLOAT")]
    [InlineData("ENTITY_INDEX")]
    [InlineData("TEXT_LABEL_63")]
    [InlineData("ANY")]
    [InlineData("VECTOR")]
    public void TypesNotAllowedInIfCondition(string conditionType)
    {
        var s = Analyze(
            @$"PROC foo()
                {conditionType} a
                IF a
                ENDIF
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticCannotConvertType, (3, 20), (3, 20), s.Diagnostics);
    }

    [Theory]
    [InlineData("STRING")]
    [InlineData("FLOAT")]
    [InlineData("ENTITY_INDEX")]
    [InlineData("TEXT_LABEL_63")]
    [InlineData("ANY")]
    [InlineData("VECTOR")]
    public void TypesNotAllowedInWhileCondition(string conditionType)
    {
        var s = Analyze(
            @$"PROC foo()
                {conditionType} a
                WHILE a
                ENDWHILE
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticCannotConvertType, (3, 23), (3, 23), s.Diagnostics);
    }
}

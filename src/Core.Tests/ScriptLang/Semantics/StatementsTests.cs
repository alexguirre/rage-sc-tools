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

    [Fact]
    public void GotoWithTargetToItselfIsResolvedCorrectly()
    {
        var (s, ast) = AnalyzeAndAst(
            @$"PROC foo()
                label:
                    GOTO label
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var @goto = (GotoStatement)((FunctionDeclaration)ast.Declarations[0]).Body[0];
        NotNull(@goto.Label);
        Equal("label", @goto.Label!.Name);
        Equal("label", @goto.TargetLabel);
        Same(@goto, @goto.Semantics.Target);
    }

    [Fact]
    public void GotoWithTargetBeforeIsResolvedCorrectly()
    {
        var (s, ast) = AnalyzeAndAst(
            @$"PROC foo()
                label:
                    IF TRUE
                    ENDIF

                    GOTO label
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var @if = (IfStatement)((FunctionDeclaration)ast.Declarations[0]).Body[0];
        var @goto = (GotoStatement)((FunctionDeclaration)ast.Declarations[0]).Body[1];
        NotNull(@if.Label);
        Equal("label", @if.Label!.Name);
        Null(@goto.Label);
        Equal("label", @goto.TargetLabel);
        Same(@if, @goto.Semantics.Target);
    }

    [Fact]
    public void GotoWithTargetAfterIsResolvedCorrectly()
    {
        var (s, ast) = AnalyzeAndAst(
            @$"PROC foo()
                    GOTO label

                label:
                    IF TRUE
                    ENDIF
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var @goto = (GotoStatement)((FunctionDeclaration)ast.Declarations[0]).Body[0];
        var @if = (IfStatement)((FunctionDeclaration)ast.Declarations[0]).Body[1];
        NotNull(@if.Label);
        Equal("label", @if.Label!.Name);
        Null(@goto.Label);
        Equal("label", @goto.TargetLabel);
        Same(@if, @goto.Semantics.Target);
    }

    [Fact]
    public void GotoWithTargetEmptyStatementIsResolvedCorrectly()
    {
        var (s, ast) = AnalyzeAndAst(
            @$"PROC foo()
                    GOTO label
                    GOTO nestedLabel

                    IF TRUE
                    nestedLabel:
                    ENDIF

                label:
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var @goto = (GotoStatement)((FunctionDeclaration)ast.Declarations[0]).Body[0];
        var gotoNested = (GotoStatement)((FunctionDeclaration)ast.Declarations[0]).Body[1];
        var emptyStmt = (EmptyStatement)((FunctionDeclaration)ast.Declarations[0]).Body[^1];
        var nestedEmptyStmt = (EmptyStatement)((IfStatement)((FunctionDeclaration)ast.Declarations[0]).Body[2]).Then[0];

        NotNull(emptyStmt.Label);
        Equal("label", emptyStmt.Label!.Name);

        NotNull(nestedEmptyStmt.Label);
        Equal("nestedLabel", nestedEmptyStmt.Label!.Name);

        Null(@goto.Label);
        Equal("label", @goto.TargetLabel);
        Same(emptyStmt, @goto.Semantics.Target);

        Null(gotoNested.Label);
        Equal("nestedLabel", gotoNested.TargetLabel);
        Same(nestedEmptyStmt, gotoNested.Semantics.Target);
    }

    [Fact]
    public void GotoWithUndefinedLabel()
    {
        var (s, _) = AnalyzeAndAst(
            @$"PROC foo()
                    GOTO label
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticUndefinedLabel, (2, 26), (2, 30), s.Diagnostics);
    }

    [Fact]
    public void GotoCannotTargetLabelInDifferentFunction()
    {
        var (s, _) = AnalyzeAndAst(
            @$"PROC bar()
                 label:
              ENDPROC

              PROC foo()
                    GOTO label
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticUndefinedLabel, (6, 26), (6, 30), s.Diagnostics);
    }
}

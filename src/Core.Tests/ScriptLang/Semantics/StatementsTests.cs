namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

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
        var whileStmt = ast.FindFirstNodeOfType<WhileStatement>();
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
        var whileStmt = ast.FindFirstNodeOfType<WhileStatement>();
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
        var whileStmt = ast.FindFirstNodeOfType<WhileStatement>();
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
        var whileStmt = ast.FindFirstNodeOfType<WhileStatement>();
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
        var whileStmt = ast.FindFirstNodeOfType<WhileStatement>();
        True(whileStmt.Condition.Semantics is { Type: BoolType, ValueKind: ValueKind.RValue | ValueKind.Assignable | ValueKind.Addressable });
    }

    [Fact]
    public void BreakInsideWhileLoop()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo()
                WHILE TRUE
                    BREAK
                ENDWHILE
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var whileStmt = ast.FindFirstNodeOfType<WhileStatement>();
        var breakStmt = whileStmt.FindFirstNodeOfType<BreakStatement>();
        True(whileStmt.Condition.Semantics is { Type: BoolType, ValueKind: ValueKind.RValue | ValueKind.Constant });
        True(whileStmt.Semantics is { ExitLabel: not null, BeginLabel: not null, ContinueLabel: not null });
        Same(whileStmt, breakStmt.Semantics.EnclosingStatement);
    }

    [Fact]
    public void RepeatLoop()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo()
                REPEAT 10 i
                ENDREPEAT
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var repStmt = ast.FindFirstNodeOfType<RepeatStatement>();
        True(repStmt.Limit.Semantics is { Type: IntType, ValueKind: ValueKind.RValue | ValueKind.Constant });
        True(repStmt.Counter.Semantics is { Type: IntType, ValueKind: ValueKind.RValue | ValueKind.Addressable | ValueKind.Assignable, Symbol: VarDeclaration { Kind: VarKind.Local } });
    }

    [Fact]
    public void RepeatLoopWorksWithNonConstantLimit()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo()
                INT n = 10
                REPEAT n i
                ENDREPEAT
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var nVarDecl = ast.FindFirstNodeOfType<VarDeclaration>();
        var repStmt = ast.FindFirstNodeOfType<RepeatStatement>();
        True(repStmt.Limit.Semantics is { Type: IntType, ValueKind: ValueKind.RValue | ValueKind.Addressable | ValueKind.Assignable });
        Equal(nVarDecl, ((NameExpression)repStmt.Limit).Semantics.Symbol);
        True(repStmt.Counter.Semantics is { Type: IntType, ValueKind: ValueKind.RValue | ValueKind.Addressable | ValueKind.Assignable, Symbol: VarDeclaration { Kind: VarKind.Local } });
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
        var ifStmt = ast.FindFirstNodeOfType<IfStatement>();
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
        var ifStmt = ast.FindFirstNodeOfType<IfStatement>();
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
        var ifStmt = ast.FindFirstNodeOfType<IfStatement>();
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
        var ifStmt = ast.FindFirstNodeOfType<IfStatement>();
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
        var ifStmt = ast.FindFirstNodeOfType<IfStatement>();
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
        var @goto = ast.FindFirstNodeOfType<GotoStatement>();
        NotNull(@goto.Label);
        Equal("label", @goto.Label!.Name);
        Equal("label", @goto.TargetLabel);
        Same(@goto.Label, @goto.Semantics.Target);
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
        var @if = ast.FindFirstNodeOfType<IfStatement>();
        var @goto = ast.FindFirstNodeOfType<GotoStatement>();
        NotNull(@if.Label);
        Equal("label", @if.Label!.Name);
        Null(@goto.Label);
        Equal("label", @goto.TargetLabel);
        Same(@if.Label, @goto.Semantics.Target);
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
        var @if = ast.FindFirstNodeOfType<IfStatement>();
        var @goto = ast.FindFirstNodeOfType<GotoStatement>();
        NotNull(@if.Label);
        Equal("label", @if.Label!.Name);
        Null(@goto.Label);
        Equal("label", @goto.TargetLabel);
        Same(@if.Label, @goto.Semantics.Target);
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
        var @goto = ast.FindNthNodeOfType<GotoStatement>(1);
        var gotoNested = ast.FindNthNodeOfType<GotoStatement>(2);
        var emptyStmt = ast.FindNthNodeOfType<EmptyStatement>(2);
        var nestedEmptyStmt = ast.FindNthNodeOfType<EmptyStatement>(1);

        NotNull(emptyStmt.Label);
        Equal("label", emptyStmt.Label!.Name);

        NotNull(nestedEmptyStmt.Label);
        Equal("nestedLabel", nestedEmptyStmt.Label!.Name);

        Null(@goto.Label);
        Equal("label", @goto.TargetLabel);
        Same(emptyStmt.Label, @goto.Semantics.Target);

        Null(gotoNested.Label);
        Equal("nestedLabel", gotoNested.TargetLabel);
        Same(nestedEmptyStmt.Label, gotoNested.Semantics.Target);
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

    [Fact]
    public void CanReturnValueWithSameType()
    {
        var (s, _) = AnalyzeAndAst(
            @"FUNC INT BAR()
                RETURN 10
            ENDFUNC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CanReturnValueWithPromotableType()
    {
        var (s, _) = AnalyzeAndAst(
            @"FUNC BOOL BAR()
                RETURN 10
            ENDFUNC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CanReturnReferenceWithSameType()
    {
        var (s, _) = AnalyzeAndAst(
            @"FUNC INT BAR(INT& a)
                RETURN a
            ENDFUNC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CanReturnReferenceWithPromotableType()
    {
        var (s, _) = AnalyzeAndAst(
            @"FUNC BOOL BAR(INT& a)
                RETURN a
            ENDFUNC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CannotReturnMismatchedTypes()
    {
        var (s, _) = AnalyzeAndAst(
            @"FUNC INT BAR()
                RETURN 'hello'
            ENDFUNC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticCannotConvertType, (2, 24), (2, 30), s.Diagnostics);
    }

    [Fact]
    public void CannotReturnReferenceWithDifferentType()
    {
        var (s, _) = AnalyzeAndAst(
            @"FUNC STRING BAR(INT& a)
                RETURN a
            ENDFUNC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticCannotConvertType, (2, 24), (2, 24), s.Diagnostics);
    }

    [Fact]
    public void CannotReturnWithoutValueInsideFunction()
    {
        var (s, _) = AnalyzeAndAst(
            @"FUNC INT BAR()
                RETURN
            ENDFUNC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticExpectedValueInReturn, (2, 17), (2, 22), s.Diagnostics);
    }

    [Fact]
    public void CanReturnWithoutValueInsideProcedure()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC BAR()
                RETURN
            ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CannotReturnWithValueInsideProcedure()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC BAR()
                RETURN 123
            ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticValueReturnedFromProcedure, (2, 24), (2, 26), s.Diagnostics);
    }

    [Fact]
    public void BreakInsideSwitchStatement()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo()
                SWITCH 1
                CASE 1
                    BREAK
                ENDSWITCH
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var switchStmt = ast.FindFirstNodeOfType<SwitchStatement>();
        var case1 = (ValueSwitchCase)switchStmt.Cases[0];
        var breakStmt = (BreakStatement)case1.Body[0];
        True(switchStmt.Expression.Semantics is { Type: IntType, ValueKind: ValueKind.RValue | ValueKind.Constant });
        True(switchStmt.Semantics is { ExitLabel: not null });
        True(case1.Semantics is { Label: not null });
        Same(switchStmt, breakStmt.Semantics.EnclosingStatement);
    }

    [Fact]
    public void DuplicateSwitchCaseIsNotAllowed()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo()
                SWITCH 1
                CASE 1
                    BREAK
                CASE 1
                    BREAK
                ENDSWITCH
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticDuplicateSwitchCase, (5, 22), (5, 22), s.Diagnostics);
    }

    [Fact]
    public void DuplicateSwitchDefaultCaseIsNotAllowed()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo()
                SWITCH 1
                DEFAULT
                    BREAK
                DEFAULT
                    BREAK
                ENDSWITCH
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticDuplicateSwitchCase, (5, 17), (5, 23), s.Diagnostics);
    }

    [Fact]
    public void SwitchWorksWithInts()
    {
        var (s, ast) = AnalyzeAndAst(
            @"PROC foo(INT intValue)
                SWITCH intValue
                CASE 0
                    BREAK
                CASE 1
                    BREAK
                CASE 2+3
                    BREAK
                DEFAULT
                    BREAK
                ENDSWITCH
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var switchStmt = ast.FindFirstNodeOfType<SwitchStatement>();
        var case1 = (ValueSwitchCase)switchStmt.Cases[0];
        var case2 = (ValueSwitchCase)switchStmt.Cases[1];
        var case3 = (ValueSwitchCase)switchStmt.Cases[2];
        var case4 = (DefaultSwitchCase)switchStmt.Cases[3];
        var breakStmt1 = (BreakStatement)case1.Body[0];
        var breakStmt2 = (BreakStatement)case2.Body[0];
        var breakStmt3 = (BreakStatement)case3.Body[0];
        var breakStmt4 = (BreakStatement)case4.Body[0];
        Equal(IntType.Instance, switchStmt.Expression.Type);
        Equal(IntType.Instance, switchStmt.Semantics.SwitchType);
        True(switchStmt.Semantics is { ExitLabel: not null });
        True(case1.Semantics is { Label: not null, Value: 0 });
        True(case2.Semantics is { Label: not null, Value: 1 });
        True(case3.Semantics is { Label: not null, Value: 5 });
        True(case4.Semantics is { Label: not null, Value: null });
        Same(switchStmt, breakStmt1.Semantics.EnclosingStatement);
        Same(switchStmt, breakStmt2.Semantics.EnclosingStatement);
        Same(switchStmt, breakStmt3.Semantics.EnclosingStatement);
        Same(switchStmt, breakStmt4.Semantics.EnclosingStatement);
    }

    [Theory]
    [InlineData("FLOAT")]
    [InlineData("BOOL")]
    [InlineData("STRING")]
    [InlineData("VECTOR")]
    [InlineData("ANY")]
    [InlineData("TEXT_LABEL_63")]
    [InlineData("ENTITY_INDEX")]
    public void SwitchDoesNotWorkWithOtherTypes(string typeStr)
    {
        var (s, _) = AnalyzeAndAst(
            $@"PROC foo({typeStr} v)
                SWITCH v
                ENDSWITCH
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticTypeNotAllowedInSwitch, (2, 24), (2, 24), s.Diagnostics);
    }

    [Fact]
    public void SwitchDoesNotWorkWithArrays()
    {
        var (s, _) = AnalyzeAndAst(
            $@"PROC foo()
                INT arr[10]
                SWITCH arr
                ENDSWITCH
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticTypeNotAllowedInSwitch, (3, 24), (3, 26), s.Diagnostics);
    }

    [Fact]
    public void SwitchDoesNotWorkWithStructs()
    {
        var (s, _) = AnalyzeAndAst(
            $@"STRUCT MY_DATA
                INT x
              ENDSTRUCT

              PROC foo(MY_DATA d)
                SWITCH d
                ENDSWITCH
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticTypeNotAllowedInSwitch, (6, 24), (6, 24), s.Diagnostics);
    }

    [Fact]
    public void SwitchDoesNotWorkWithFunctionTypeDefs()
    {
        var (s, _) = AnalyzeAndAst(
            $@"TYPEDEF FUNC INT MY_FUNC()

              PROC foo(MY_FUNC f)
                SWITCH f
                ENDSWITCH
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticTypeNotAllowedInSwitch, (4, 24), (4, 24), s.Diagnostics);
    }

    [Fact]
    public void SwitchWorksWithEnums()
    {
        var (s, ast) = AnalyzeAndAst(
            @"ENUM BAR
                BAR_A
                BAR_B
              ENDENUM

              PROC foo(BAR enumValue)
                SWITCH enumValue
                CASE BAR_A
                    BREAK
                CASE BAR_B
                    BREAK
                CASE INT_TO_ENUM(BAR, 2)
                    BREAK
                DEFAULT
                    BREAK
                ENDSWITCH
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
        var enumDecl = ast.FindFirstNodeOfType<EnumDeclaration>();
        var switchStmt = ast.FindFirstNodeOfType<SwitchStatement>();
        var case1 = (ValueSwitchCase)switchStmt.Cases[0];
        var case2 = (ValueSwitchCase)switchStmt.Cases[1];
        var case3 = (ValueSwitchCase)switchStmt.Cases[2];
        var case4 = (DefaultSwitchCase)switchStmt.Cases[3];
        var breakStmt1 = (BreakStatement)case1.Body[0];
        var breakStmt2 = (BreakStatement)case2.Body[0];
        var breakStmt3 = (BreakStatement)case3.Body[0];
        var breakStmt4 = (BreakStatement)case4.Body[0];
        Equal(enumDecl.DeclaredType, switchStmt.Expression.Type);
        Equal(enumDecl.DeclaredType, switchStmt.Semantics.SwitchType);
        True(switchStmt.Semantics is { ExitLabel: not null });
        True(case1.Semantics is { Label: not null, Value: 0 });
        True(case2.Semantics is { Label: not null, Value: 1 });
        True(case3.Semantics is { Label: not null, Value: 2 });
        True(case4.Semantics is { Label: not null, Value: null });
        Same(switchStmt, breakStmt1.Semantics.EnclosingStatement);
        Same(switchStmt, breakStmt2.Semantics.EnclosingStatement);
        Same(switchStmt, breakStmt3.Semantics.EnclosingStatement);
        Same(switchStmt, breakStmt4.Semantics.EnclosingStatement);
    }

    [Fact]
    public void SwitchCaseWithDifferentEnumTypeIsNotAllowed()
    {
        var (s, ast) = AnalyzeAndAst(
            @"ENUM BAR
                BAR_A
              ENDENUM

              ENUM BAZ
                BAZ_A
              ENDENUM

              PROC foo(BAR enumValue)
                SWITCH enumValue
                CASE BAR_A
                    BREAK
                CASE BAZ_A
                    BREAK
                ENDSWITCH
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticCannotConvertType, (13, 22), (13, 26), s.Diagnostics);
    }

    [Fact]
    public void SwitchCaseWithIntWhenSwitchingOnEnumTypeIsNotAllowed()
    {
        var (s, ast) = AnalyzeAndAst(
            @"ENUM BAR
                BAR_A
              ENDENUM

              PROC foo(BAR enumValue)
                SWITCH enumValue
                CASE BAR_A
                    BREAK
                CASE 1
                    BREAK
                ENDSWITCH
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticCannotConvertType, (9, 22), (9, 22), s.Diagnostics);
    }
}

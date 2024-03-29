﻿namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;

public class FunctionTests : SemanticsTestsBase
{
    [Fact]
    public void OptionalParameters()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo(INT v = 123)
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void ErrorOnRequiredParameterAfterOptionalParameter()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo(INT v = 123, INT v2, INT v3 = 456)
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticRequiredParameterAfterOptionalParameter, (1, 23), (1, 28), s.Diagnostics);
    }

    [Fact]
    public void CallWithOptionalParameter()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo(INT v = 123)
              ENDPROC

              PROC bar()
                foo()
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CallWithMultipleOptionalParameters()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo(INT v = 123, INT v2 = 456)
              ENDPROC

              PROC bar()
                foo()
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CallWithValuesPassedToOptionalParameters()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo(INT v = 123, INT v2 = 456)
              ENDPROC

              PROC bar()
                foo(789, 321)
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CallWithValuesPassedToOptionalParametersPartially()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo(INT v = 123, INT v2 = 456)
              ENDPROC

              PROC bar()
                foo(789)
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CallWithRequiredParametersAndOptionalParameters()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo(INT v, INT v2 = 456)
              ENDPROC

              PROC bar()
                foo(789)
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void CallWithRequiredParametersAndValuesPassedToOptionalParameters()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo(INT v, INT v2 = 456)
              ENDPROC

              PROC bar()
                foo(789, 321)
              ENDPROC"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void ErrorOnMissingRequiredParameter()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo(INT v, INT v2)
              ENDPROC

              PROC bar()
                foo(789)
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticMissingRequiredParameter, (5, 17), (5, 19), s.Diagnostics);
    }

    [Fact]
    public void ErrorOnTooManyArguments()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo(INT v)
              ENDPROC

              PROC bar()
                foo(123, 456, 789)
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticTooManyArguments, (5, 26), (5, 33), s.Diagnostics);
    }

    [Fact]
    public void OptionalParametersOnNativeFunctionsAreAllowed()
    {
        var (s, _) = AnalyzeAndAst(
            @"
                NATIVE FUNC INT foo1(INT v1, INT v2, INT v3 = 123, INT v4 = 456)
                NATIVE PROC foo2(INT v1, INT v2, INT v3 = 123, INT v4 = 456)"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void OptionalParametersOnFunctionTypeDefsAreAllowed()
    {
        var (s, _) = AnalyzeAndAst(
            @"
                TYPEDEF FUNC INT foo1(INT v1, INT v2, INT v3 = 123, INT v4 = 456)
                TYPEDEF PROC foo2(INT v1, INT v2, INT v3 = 123, INT v4 = 456)"
        );

        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void ErrorOnMultipleScriptDeclarations()
    {
        var (s, _) = AnalyzeAndAst(
            @"
                SCRIPT a
                ENDSCRIPT

                SCRIPT b
                ENDSCRIPT

                SCRIPT c
                ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticMultipleScriptDeclarations, (5, 17), (6, 25), s.Diagnostics);
        CheckError(ErrorCode.SemanticMultipleScriptDeclarations, (8, 17), (9, 25), s.Diagnostics);
    }
}

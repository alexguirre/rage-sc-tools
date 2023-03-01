namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;

public class ExpressionTests : SemanticsTestsBase
{
    [Fact]
    public void PostfixIncrementAndDecrement()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo()
                INT n
                INT i = n++
                INT k = n--
              ENDPROC"
        );
        False(s.Diagnostics.HasErrors);
    }
    
    [Fact]
    public void PostfixIncrementAndDecrementAsStatements()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo()
                INT n
                n++
                n--
              ENDPROC"
        );
        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void ErrorOnPostfixIncrementAndDecrementWithNonInt()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo()
                BOOL b
                b++
                b--
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticBadPostfixUnaryOp, (3, 17), (3, 19), s.Diagnostics);
        CheckError(ErrorCode.SemanticBadPostfixUnaryOp, (4, 17), (4, 19), s.Diagnostics);
    }

    [Fact]
    public void ErrorOnPostfixIncrementAndDecrementWithNonAddressableExpression()
    {
        var (s, _) = AnalyzeAndAst(
            @"PROC foo()
                1++
                1--
              ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticExpressionIsNotAssignable, (2, 17), (2, 17), s.Diagnostics);
        CheckError(ErrorCode.SemanticExpressionIsNotAssignable, (3, 17), (3, 17), s.Diagnostics);
    }
}

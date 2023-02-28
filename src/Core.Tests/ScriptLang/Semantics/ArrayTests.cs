namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;

public class ArrayTests : SemanticsTestsBase
{
    [Fact]
    public void ArraySizeMustBeConstant()
    {
        var s = Analyze(
            @"INT n
              INT foo[n]"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticArrayLengthExpressionIsNotConstant, (2, 23), (2, 23), s.Diagnostics);
    }

    [Fact]
    public void ArraySizeMustBeInt()
    {
        var s = Analyze(
            @"INT foo[TRUE]"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticCannotConvertType, (1, 9), (1, 12), s.Diagnostics);
    }

    [Fact]
    public void MultidimensionalArrays()
    {
        var s = Analyze(
            @"PROC foo(INT &a[2][4][6])
                INT n = a[0][1][2]
            ENDPROC"
        );
        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void IncompleteArrayAsParameter()
    {
        var s = Analyze(
            @"PROC foo(INT &a[])
            ENDPROC"
        );
        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void IncompleteArrayWithArrayAsItemType()
    {
        var s = Analyze(
            @"PROC foo(INT &a[][10])
                INT n = a[0][1]
            ENDPROC"
        );
        False(s.Diagnostics.HasErrors);
    }

    [Fact]
    public void ErrorOnIncompleteArrayWithIncompleteArrayAsItemType()
    {
        var s = Analyze(
            @"PROC foo(INT &a[][])
            ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticArrayItemTypeIsIncompleteArray, (1, 16), (1, 19), s.Diagnostics);
    }

    [Fact]
    public void ErrorOnArrayWithIncompleteArrayAsItemType()
    {
        var s = Analyze(
            @"PROC foo(INT &a[5][])
            ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticArrayItemTypeIsIncompleteArray, (1, 16), (1, 20), s.Diagnostics);
    }

    [Fact]
    public void ErrorOnIncompleteArrayPassedByValue()
    {
        var s = Analyze(
            @"PROC foo(INT a[])
            ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticIncompleteArrayNotByRef, (1, 10), (1, 16), s.Diagnostics);
    }

    [Fact]
    public void ErrorOnIncompleteArrayInStatic()
    {
        var s = Analyze(
            @"INT foo[]"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticIncompleteArrayNotAllowed, (1, 8), (1, 9), s.Diagnostics);
    }

    [Fact]
    public void ErrorOnIncompleteArrayInLocal()
    {
        var s = Analyze(
            @"PROC bar()
                INT foo[]
            ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticIncompleteArrayNotAllowed, (2, 24), (2, 25), s.Diagnostics);
    }

    [Fact]
    public void ErrorOnIncompleteArrayInStructField()
    {
        var s = Analyze(
            @"STRUCT DATA
                INT foo[]
            ENDSTRUCT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticIncompleteArrayNotAllowed, (2, 24), (2, 25), s.Diagnostics);
    }

    [Fact]
    public void ErrorOnIncompleteArrayInScriptParameter()
    {
        var s = Analyze(
            @"SCRIPT(INT &foo[])
            ENDSCRIPT"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticIncompleteArrayNotAllowed, (1, 16), (1, 17), s.Diagnostics);
    }

    [Fact]
    public void ErrorOnNonIntIndex()
    {
        var s = Analyze(
            @"PROC bar()
                INT foo[5]
                INT n1 = foo[TRUE]
                INT n2 = foo[1.0]
                INT n3 = foo['hello']
                INT n4 = foo[<<1.0, 2.0, 3.0>>]
            ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticInvalidIndexType, (3, 30), (3, 33), s.Diagnostics);
        CheckError(ErrorCode.SemanticInvalidIndexType, (4, 30), (4, 32), s.Diagnostics);
        CheckError(ErrorCode.SemanticInvalidIndexType, (5, 30), (5, 36), s.Diagnostics);
        CheckError(ErrorCode.SemanticInvalidIndexType, (6, 30), (6, 46), s.Diagnostics);
    }

    [Fact]
    public void ErrorOnIndexingNonArrayType()
    {
        var s = Analyze(
            @"PROC bar()
                INT n1 = 1[0]
                INT n2 = TRUE[0]
                INT n3 = 1.0[0]
                INT n4 = 'hello'[0]
                INT n5 = <<1.0, 2.0, 3.0>>[0]
            ENDPROC"
        );

        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticIndexingNotSupported, (2, 26), (2, 29), s.Diagnostics);
        CheckError(ErrorCode.SemanticIndexingNotSupported, (3, 26), (3, 32), s.Diagnostics);
        CheckError(ErrorCode.SemanticIndexingNotSupported, (4, 26), (4, 31), s.Diagnostics);
        CheckError(ErrorCode.SemanticIndexingNotSupported, (5, 26), (5, 35), s.Diagnostics);
        CheckError(ErrorCode.SemanticIndexingNotSupported, (6, 26), (6, 45), s.Diagnostics);
    }
}

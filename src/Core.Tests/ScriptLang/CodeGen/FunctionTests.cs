namespace ScTools.Tests.ScriptLang.CodeGen;

public class FunctionTests : CodeGenTestsBase
{
    [Fact]
    public void CallWithOptionalParameter()
    {
        CompileScript(
        scriptSource: @"
                foo()
            ",
        declarationsSource: @"
                PROC foo(INT v = 123)
                ENDPROC
            ",
        expectedAssembly: $@"
                ENTER 0, 2

                ; foo()
                {IntToPushInst(123)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 1, 3
                LEAVE 1, 0
            ");
    }

    [Fact]
    public void CallWithMultipleOptionalParameters()
    {
        CompileScript(
        scriptSource: @"
                foo()
            ",
        declarationsSource: @"
                PROC foo(INT v = 123, INT v2 = 456)
                ENDPROC
            ",
        expectedAssembly: $@"
                ENTER 0, 2

                ; foo()
                {IntToPushInst(123)}
                {IntToPushInst(456)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ");
    }

    [Fact]
    public void CallWithValuesPassedToOptionalParameters()
    {
        CompileScript(
        scriptSource: @"
                foo(789, 321)
            ",
        declarationsSource: @"
                PROC foo(INT v = 123, INT v2 = 456)
                ENDPROC
            ",
        expectedAssembly: $@"
                ENTER 0, 2

                ; foo(789, 321)
                {IntToPushInst(789)}
                {IntToPushInst(321)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ");
    }

    [Fact]
    public void CallWithValuesPassedToOptionalParametersPartially()
    {
        CompileScript(
        scriptSource: @"
                foo(789)
            ",
        declarationsSource: @"
                PROC foo(INT v = 123, INT v2 = 456)
                ENDPROC
            ",
        expectedAssembly: $@"
                ENTER 0, 2

                ; foo(789)
                {IntToPushInst(789)}
                {IntToPushInst(456)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ");
    }

    [Fact]
    public void CallWithRequiredParametersAndOptionalParameters()
    {
        CompileScript(
        scriptSource: @"
                foo(789)
            ",
        declarationsSource: @"
                PROC foo(INT v, INT v2 = 456)
                ENDPROC
            ",
        expectedAssembly: $@"
                ENTER 0, 2

                ; foo(789)
                {IntToPushInst(789)}
                {IntToPushInst(456)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ");
    }

    [Fact]
    public void CallWithRequiredParametersAndValuesPassedToOptionalParameters()
    {
        CompileScript(
        scriptSource: @"
                foo(789, 321)
            ",
        declarationsSource: @"
                PROC foo(INT v, INT v2 = 456)
                ENDPROC
            ",
        expectedAssembly: $@"
                ENTER 0, 2

                ; foo(789, 321)
                {IntToPushInst(789)}
                {IntToPushInst(321)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ");
    }
}

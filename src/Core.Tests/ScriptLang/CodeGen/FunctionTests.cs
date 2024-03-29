﻿namespace ScTools.Tests.ScriptLang.CodeGen;

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
            ",
        expectedAssemblyIV: $@"
                ENTER 0, 2

                ; foo()
                {IntToPushInstIV(123)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 1, 3
                LEAVE 1, 0
            ",
        expectedAssemblyMP3: $@"
                ENTER 0, 2

                ; foo()
                {IntToPushInstMP3(123)}
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
            ",
        expectedAssemblyIV: $@"
                ENTER 0, 2

                ; foo()
                {IntToPushInstIV(123)}
                {IntToPushInstIV(456)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ",
        expectedAssemblyMP3: $@"
                ENTER 0, 2

                ; foo()
                {IntToPushInstMP3(123)}
                {IntToPushInstMP3(456)}
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
            ",
        expectedAssemblyIV: $@"
                ENTER 0, 2

                ; foo(789, 321)
                {IntToPushInstIV(789)}
                {IntToPushInstIV(321)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ",
        expectedAssemblyMP3: $@"
                ENTER 0, 2

                ; foo(789, 321)
                {IntToPushInstMP3(789)}
                {IntToPushInstMP3(321)}
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
            ",
        expectedAssemblyIV: $@"
                ENTER 0, 2

                ; foo(789)
                {IntToPushInstIV(789)}
                {IntToPushInstIV(456)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ",
        expectedAssemblyMP3: $@"
                ENTER 0, 2

                ; foo(789)
                {IntToPushInstMP3(789)}
                {IntToPushInstMP3(456)}
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
            ",
        expectedAssemblyIV: $@"
                ENTER 0, 2

                ; foo(789)
                {IntToPushInstIV(789)}
                {IntToPushInstIV(456)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ",
        expectedAssemblyMP3: $@"
                ENTER 0, 2

                ; foo(789)
                {IntToPushInstMP3(789)}
                {IntToPushInstMP3(456)}
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
            ",
        expectedAssemblyIV: $@"
                ENTER 0, 2

                ; foo(789, 321)
                {IntToPushInstIV(789)}
                {IntToPushInstIV(321)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ",
        expectedAssemblyMP3: $@"
                ENTER 0, 2

                ; foo(789, 321)
                {IntToPushInstMP3(789)}
                {IntToPushInstMP3(321)}
                CALL foo

                LEAVE 0, 0

            foo:
                ENTER 2, 4
                LEAVE 2, 0
            ");
    }



    [Fact]
    public void NativeProcedureInvocation()
    {
        CompileScript(
        scriptSource: @"
                TEST(123, 456)
            ",
        declarationsSource: @"
                NATIVE PROC TEST(INT a, INT b) = '0x1234ABCD'
            ",
        expectedAssembly: $@"
                ENTER 0, 2

                ; TEST(123, 456)
                {IntToPushInst(123)}
                {IntToPushInst(456)}
                NATIVE 2, 0, TEST

                LEAVE 0, 0

              .include
                TEST: .native 0x1234ABCD
            ",
        expectedAssemblyIV: $@"
                ENTER 0, 2

                ; TEST(123, 456)
                {IntToPushInstIV(123)}
                {IntToPushInstIV(456)}
                NATIVE 2, 0, 0x1234ABCD

                LEAVE 0, 0
            ",
        expectedAssemblyMP3: $@"
                ENTER 0, 2

                ; TEST(123, 456)
                {IntToPushInstMP3(123)}
                {IntToPushInstMP3(456)}
                NATIVE 2, 0, 0x1234ABCD

                LEAVE 0, 0
            ");
    }

    [Fact]
    public void NativeFunctionInvocation()
    {
        CompileScript(
        scriptSource: @"
                INT n = TEST(123, 456)
                TEST(123, 456)
            ",
        declarationsSource: @"
                NATIVE FUNC INT TEST(INT a, INT b) = '0x1234ABCD'
            ",
        expectedAssembly: $@"
                ENTER 0, 3

                ; INT n = TEST(123, 456)
                {IntToPushInst(123)}
                {IntToPushInst(456)}
                NATIVE 2, 1, TEST
                LOCAL_U8_STORE 2

                ; TEST(123, 456)
                {IntToPushInst(123)}
                {IntToPushInst(456)}
                NATIVE 2, 1, TEST
                DROP

                LEAVE 0, 0

              .include
                TEST: .native 0x1234ABCD
            ",
        expectedAssemblyIV: $@"
                ENTER 0, 3

                ; INT n = TEST(123, 456)
                {IntToPushInstIV(123)}
                {IntToPushInstIV(456)}
                NATIVE 2, 1, 0x1234ABCD
                LOCAL_2
                STORE

                ; TEST(123, 456)
                {IntToPushInstIV(123)}
                {IntToPushInstIV(456)}
                NATIVE 2, 1, 0x1234ABCD
                DROP

                LEAVE 0, 0
            ",
        expectedAssemblyMP3: $@"
                ENTER 0, 3

                ; INT n = TEST(123, 456)
                {IntToPushInstMP3(123)}
                {IntToPushInstMP3(456)}
                NATIVE 2, 1, 0x1234ABCD
                LOCAL_2
                STORE

                ; TEST(123, 456)
                {IntToPushInstMP3(123)}
                {IntToPushInstMP3(456)}
                NATIVE 2, 1, 0x1234ABCD
                DROP

                LEAVE 0, 0
            ");
    }
}

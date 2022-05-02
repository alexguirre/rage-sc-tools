namespace ScTools.Tests.ScriptLang.CodeGen;

using Xunit;

public class OptimizationsTests : CodeGenTestsBase
{
    [Fact]
    public void PushConstS16AndIAddBecomeIAddS16()
    {
        CompileScript(
        scriptSource: @"
                TEST(8 + 10000)
            ",
        declarationsSource: @"
                PROC TEST(INT a)
                ENDPROC
            ",
        expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_U8 8
                IADD_S16 10000
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 1, 3
                LEAVE 1, 0
            ");
    }

    [Fact]
    public void PushConstS16AndIMulBecomeIMulS16()
    {
        CompileScript(
        scriptSource: @"
                TEST(8 * 10000)
            ",
        declarationsSource: @"
                PROC TEST(INT a)
                ENDPROC
            ",
        expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_U8 8
                IMUL_S16 10000
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 1, 3
                LEAVE 1, 0
            ");
    }

    [Theory]
    [InlineData(0), InlineData(1), InlineData(2), InlineData(3)] // check that PUSH_CONST_0-PUSH_CONST_7 are detected
    [InlineData(4), InlineData(5), InlineData(6), InlineData(7)]
    [InlineData(200)] // check that PUSH_CONST_U8 is detected
    public void PushConstU8AndIAddBecomeIAddU8(byte value)
    {
        CompileScript(
        scriptSource: @$"
                TEST(8 + {value})
            ",
        declarationsSource: @"
                PROC TEST(INT a)
                ENDPROC
            ",
        expectedAssembly: @$"
                ENTER 0, 2
                PUSH_CONST_U8 8
                IADD_U8 {value}
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 1, 3
                LEAVE 1, 0
            ");
    }

    [Theory]
    [InlineData(0), InlineData(1), InlineData(2), InlineData(3)] // check that PUSH_CONST_0-PUSH_CONST_7 are detected
    [InlineData(4), InlineData(5), InlineData(6), InlineData(7)]
    [InlineData(200)] // check that PUSH_CONST_U8 is detected
    public void PushConstU8AndIMulBecomeIMulU8(byte value)
    {
        CompileScript(
        scriptSource: @$"
                TEST(8 * {value})
            ",
        declarationsSource: @"
                PROC TEST(INT a)
                ENDPROC
            ",
        expectedAssembly: @$"
                ENTER 0, 2
                PUSH_CONST_U8 8
                IMUL_U8 {value}
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 1, 3
                LEAVE 1, 0
            ");
    }

    [Fact]
    public void TwoPushConstU8BecomePushConstU8U8()
    {
        CompileScript(
        scriptSource: @"
                TEST(8, 11)
            ",
        declarationsSource: @"
                PROC TEST(INT a, INT b)
                ENDPROC
            ",
        expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_U8_U8 8, 11
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 2, 4
                LEAVE 2, 0
            ");
    }

    [Fact]
    public void ThreePushConstU8BecomePushConstU8U8U8()
    {
        CompileScript(
        scriptSource: @"
                TEST(8, 11, 12)
            ",
        declarationsSource: @"
                PROC TEST(INT a, INT b, INT c)
                ENDPROC
            ",
        expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_U8_U8_U8 8, 11, 12
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 3, 5
                LEAVE 3, 0
            ");
    }

    [Fact]
    public void ThreePushConstU8AndIAddBecomePushConstU8U8AndIAddU8()
    {
        CompileScript(
        scriptSource: @"
                TEST(8, 11 + 10)
            ",
        declarationsSource: @"
                PROC TEST(INT a, INT b)
                ENDPROC
            ",
        expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_U8_U8 8, 11
                IADD_U8 10
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 2, 4
                LEAVE 2, 0
            ");
    }

    [Fact]
    public void ThreePushConstU8ThenIAddAndPushConstU8BecomePushConstU8U8ThenIAddU8AndPushConstU8()
    {
        CompileScript(
        scriptSource: @"
                TEST(8, 11 + 10, 8)
            ",
        declarationsSource: @"
                PROC TEST(INT a, INT b, INT c)
                ENDPROC
            ",
        expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_U8_U8 8, 11
                IADD_U8 10
                PUSH_CONST_U8 8
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 3, 5
                LEAVE 3, 0
            ");
    }

    [Fact]
    public void FourPushConstU8AndIAddBecomePushConstU8U8U8AndIAddU8()
    {
        CompileScript(
        scriptSource: @"
                TEST(8, 11, 8 + 10)
            ",
        declarationsSource: @"
                PROC TEST(INT a, INT b, INT c)
                ENDPROC
            ",
        expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_U8_U8_U8 8, 11, 8
                IADD_U8 10
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 3, 5
                LEAVE 3, 0
            ");
    }
}


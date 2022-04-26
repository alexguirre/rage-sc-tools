namespace ScTools.Tests.ScriptLang.CodeGen;

using Xunit;

public class ParametersTests : CodeGenTestsBase
{
    [Fact]
    public void ReferenceCanBeRead()
    {
        CompileScript(
        scriptSource: @"
            INT n1
            BAR(n1)
        ",
        declarationsSource: @"
            FUNC INT BAR(INT& a)
                RETURN a
            ENDFUNC
        ",
        expectedAssembly: @"
            ENTER 0, 3
            LOCAL_U8 2
            CALL BAR
            DROP
            LEAVE 0, 0
        BAR:
            ENTER 1, 3
            LOCAL_U8_LOAD 0
            LOAD
            LEAVE 1, 1
        ");
    }

    [Fact]
    public void ReferenceCanBePassedToOtherReferenceParameter()
    {
        CompileScript(
        scriptSource: @"
            INT n1
            FOO(n1)
        ",
        declarationsSource: @"
            PROC BAR(INT& a)
            ENDPROC

            PROC FOO(INT& b)
                BAR(b)
            ENDPROC
        ",
        expectedAssembly: @"
            ENTER 0, 3
            LOCAL_U8 2
            CALL FOO
            LEAVE 0, 0
        FOO:
            ENTER 1, 3
            LOCAL_U8_LOAD 0
            CALL BAR
            LEAVE 1, 0
        BAR:
            ENTER 1, 3
            LEAVE 1, 0
        ");
    }
}


namespace ScTools.Tests.ScriptLang.CodeGen;

using Xunit;

public class LoopsTests : CodeGenTestsBase
{
    [Fact]
    public void WhileLoop()
    {
        CompileScript(
        scriptSource: @"
            WHILE TRUE
                INT n = 5
                CONTINUE
                BREAK
            ENDWHILE
        ",
        expectedAssembly: @"
            ENTER 0, 3
        while:
            PUSH_CONST_1
            JZ endwhile
            PUSH_CONST_5
            LOCAL_U8_STORE 2
            J while
            J endwhile
            J while
        endwhile:
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void RepeatLoop()
    {
        CompileScript(
        scriptSource: @"
            REPEAT 10 i
                INT n = 5
                CONTINUE
                BREAK
            ENDREPEAT
        ",
        expectedAssembly: @"
            ENTER 0, 4
            PUSH_CONST_0
            LOCAL_U8_STORE 2
        repeat:
            LOCAL_U8_LOAD 2
            PUSH_CONST_U8 10
            ILT_JZ endrepeat

            ; body
            PUSH_CONST_5
            LOCAL_U8_STORE 3
            J increment_counter
            J endrepeat

        increment_counter:
            LOCAL_U8_LOAD 2
            IADD_U8 1
            LOCAL_U8_STORE 2
            J repeat
        endrepeat:
            LEAVE 0, 0
        ");
    }
}

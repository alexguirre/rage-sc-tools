namespace ScTools.Tests.ScriptLang.CodeGen;

using Xunit;

public class GlobalsTests : CodeGenTestsBase
{
    private static int g(int block, int offset) => offset | (block << 18);
    
    [Fact]
    public void GlobalsAreInitializedInScriptEntryPoint()
    {
        CompileScript(
        scriptSource: @"
                FOO(n1, n2, v1, v2)
            ",
        declarationsSource: @"
                GLOBALS test_script 1
                    INT n1, n2 = 5
                    VECTOR v1 = <<1.0, 2.0, 3.0>>, v2
                ENDGLOBALS

                PROC FOO(INT i1, INT i2, VECTOR &vec1, VECTOR &vec2)
                ENDPROC
            ",
        expectedAssembly: $@"
                ENTER 0, 2
                ; globals initialization
                ; n2 = 5
                PUSH_CONST_5
                GLOBAL_U24_STORE {g(1, 1)}
                ; v1 = <<1.0, 2.0, 3.0>>
                PUSH_CONST_F1
                PUSH_CONST_F2
                PUSH_CONST_F3
                PUSH_CONST_3
                GLOBAL_U24 {g(1, 2)}
                STORE_N

                ; FOO(n1, n2, v1, v2)
                GLOBAL_U24_LOAD {g(1, 0)}
                GLOBAL_U24_LOAD {g(1, 1)}
                GLOBAL_U24 {g(1, 2)}
                GLOBAL_U24 {g(1, 5)}
                CALL FOO

                LEAVE 0, 0
            FOO:
                ENTER 4, 6
                LEAVE 4, 0

                .global_block 1
                .global
                .int 0, 0
                .float 0, 0, 0, 0, 0, 0
            ",
        expectedAssemblyIV: $@"
                ENTER 0, 2
                ; globals initialization
                ; n2 = 5
                PUSH_CONST_5
                {IntToGlobalIV(1)}
                STORE
                ; v1 = <<1.0, 2.0, 3.0>>
                PUSH_CONST_F 1.0
                PUSH_CONST_F 2.0
                PUSH_CONST_F 3.0
                PUSH_CONST_3
                {IntToGlobalIV(2)}
                STORE_N

                ; FOO(n1, n2, v1, v2)
                {IntToGlobalIV(0)}
                LOAD
                {IntToGlobalIV(1)}
                LOAD
                {IntToGlobalIV(2)}
                {IntToGlobalIV(5)}
                CALL FOO

                LEAVE 0, 0
            FOO:
                ENTER 4, 6
                LEAVE 4, 0

                .global_block 1
                .global
                .int 0, 0
                .float 0, 0, 0, 0, 0, 0
            ",
        expectedAssemblyMP3: $@"
                ENTER 0, 2
                ; globals initialization
                ; n2 = 5
                PUSH_CONST_5
                {IntToGlobalMP3(1)}
                STORE
                ; v1 = <<1.0, 2.0, 3.0>>
                PUSH_CONST_F 1.0
                PUSH_CONST_F 2.0
                PUSH_CONST_F 3.0
                PUSH_CONST_3
                {IntToGlobalMP3(2)}
                STORE_N

                ; FOO(n1, n2, v1, v2)
                {IntToGlobalMP3(0)}
                LOAD
                {IntToGlobalMP3(1)}
                LOAD
                {IntToGlobalMP3(2)}
                {IntToGlobalMP3(5)}
                CALL FOO

                LEAVE 0, 0
            FOO:
                ENTER 4, 6
                LEAVE 4, 0

                .global_block 1
                .global
                .int 0, 0
                .float 0, 0, 0, 0, 0, 0
            ");
    }
}

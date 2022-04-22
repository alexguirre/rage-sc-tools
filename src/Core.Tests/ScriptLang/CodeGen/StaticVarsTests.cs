namespace ScTools.Tests.ScriptLang.CodeGen;

using Xunit;

public class StaticVarsTests : CodeGenTestsBase
{
    [Fact]
    public void StaticsAreInitializedInScriptEntryPoint()
    {
        CompileScript(
        scriptSource: @"
                FOO(n1, n2, v1, v2)
            ",
        declarationsSource: @"
                INT n1, n2 = 5
                VECTOR v1 = <<1.0, 2.0, 3.0>>, v2

                PROC FOO(INT i1, INT i2, VECTOR &vec1, VECTOR &vec2)
                ENDPROC
            ",
        expectedAssembly: @"
                ENTER 0, 2
                ; static initialization
                ; n2 = 5
                PUSH_CONST_U8 5
                STATIC_U8 1
                STORE
                ; v1 = <<1.0, 2.0, 3.0>>
                PUSH_CONST_F1
                PUSH_CONST_F2
                PUSH_CONST_F3
                PUSH_CONST_3
                STATIC_U8 2
                STORE_N

                ; FOO(n1, n2, v1, v2)
                STATIC_U8 0
                LOAD
                STATIC_U8 1
                LOAD
                STATIC_U8 2
                STATIC_U8 5
                CALL FOO

                LEAVE 0, 0
            FOO:
                ENTER 4, 6
                LEAVE 4, 0

                .static
                .int 0, 0
                .float 0, 0, 0, 0, 0, 0
            ");
    }

    [Fact]
    public void StringStaticsWithInitializer()
    {
        CompileScript(
        scriptSource: @"
            FOO(s1,s2,s3,s4)
        ",
        declarationsSource: @"
            STRING s1 = NULL
            STRING s2 = 'hello world'
            STRING s3 = 'test'
            STRING s4 = 'hello world'

            PROC FOO(STRING str1, STRING str2, STRING str3, STRING str4)
            ENDPROC
        ",
        expectedAssembly: @"
            ENTER 0, 6
            ; static initialization
            PUSH_CONST_0
            STATIC_U8 0
            STORE
            PUSH_CONST_0
            STRING
            STATIC_U8 1
            STORE
            PUSH_CONST_U8 strTest
            STRING
            STATIC_U8 2
            STORE
            PUSH_CONST_0
            STRING
            STATIC_U8 3
            STORE

            ; FOO(s1,s2,s3,s4)
            STATIC_U8 0
            LOAD
            STATIC_U8 1
            LOAD
            STATIC_U8 2
            LOAD
            STATIC_U8 3
            LOAD
            CALL FOO

            LEAVE 0, 0
        FOO:
            ENTER 4, 6
            LEAVE 4, 0

            .static
            .int 0, 0, 0, 0
            .string
            strHelloWorld: .str 'hello world'
            strTest: .str 'test'
        ");
    }

    [Fact]
    public void UnreferencedStaticsAreNotIncluded()
    {
        CompileScript(
        scriptSource: @"
            ",
        declarationsSource: @"
                INT n1, n2 = 5
                VECTOR v1 = <<1.0, 2.0, 3.0>>, v2
                STRING s1 = NULL
                STRING s2 = 'hello world'
                STRING s3 = 'test'
                STRING s4 = 'hello world'
            ",
        expectedAssembly: @"
                ENTER 0, 2
                LEAVE 0, 0
            ");
    }
}


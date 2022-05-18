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
                PUSH_CONST_5
                STATIC_U8_STORE 1
                ; v1 = <<1.0, 2.0, 3.0>>
                PUSH_CONST_F1
                PUSH_CONST_F2
                PUSH_CONST_F3
                PUSH_CONST_3
                STATIC_U8 2
                STORE_N

                ; FOO(n1, n2, v1, v2)
                STATIC_U8_LOAD 0
                STATIC_U8_LOAD 1
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
    public void StaticWithNonConstantInitializerIsAllowed()
    {
        CompileScript(
        scriptSource: @"
            ",
        declarationsSource: @"
                FUNC INT FOO()
                    RETURN 123
                ENDFUNC

                INT n = FOO()
            ",
        expectedAssembly: @"
                ENTER 0, 2
                ; static initialization
                ; n = FOO()
                CALL FOO
                STATIC_U8_STORE n

                LEAVE 0, 0

            FOO:
                ENTER 0, 2
                PUSH_CONST_U8 123
                LEAVE 0, 1

                .static
            n:  .int 0
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
            ENTER 0, 2
            ; static initialization
            PUSH_CONST_0
            STATIC_U8_STORE 0
            PUSH_CONST_0
            STRING
            STATIC_U8_STORE 1
            PUSH_CONST_U8 strTest
            STRING
            STATIC_U8_STORE 2
            PUSH_CONST_0
            STRING
            STATIC_U8_STORE 3

            ; FOO(s1,s2,s3,s4)
            STATIC_U8_LOAD 0
            STATIC_U8_LOAD 1
            STATIC_U8_LOAD 2
            STATIC_U8_LOAD 3
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
    public void ReferencedStaticsAreIncluded()
    {
        CompileScript(
        scriptSource: @"
                FOO(n1, v1, s1)
            ",
        declarationsSource: @"
                INT n1
                VECTOR v1
                STRING s1

                PROC FOO(INT i, VECTOR &vec, STRING s)
                ENDPROC
            ",
        expectedAssembly: @"
                ENTER 0, 2

                ; FOO(n1, v1, s1)
                STATIC_U8_LOAD n1
                STATIC_U8 v1
                STATIC_U8_LOAD s1
                CALL FOO

                LEAVE 0, 0

            FOO:
                ENTER 3, 5
                LEAVE 3, 0

            .static
            n1: .int 0
            v1: .float 0, 0, 0
            s1: .int 0
            ");
    }

    [Fact]
    public void UnreferencedStaticsAreIncluded()
    {
        CompileScript(
        scriptSource: @"
            ",
        declarationsSource: @"
                INT n1
                VECTOR v1
                STRING s1
            ",
        expectedAssembly: @"
                ENTER 0, 2
                LEAVE 0, 0

            .static
            n1: .int 0
            v1: .float 0, 0, 0
            s1: .int 0
            ");
    }

    [Fact]
    public void StructWithDefaultInitializer()
    {
        CompileScript(
        scriptSource: @"
            FOO(d)
        ",
        declarationsSource: @"
            STRUCT DATA
                INT a
                INT b = -1
                INT c = -1
                INT d = -1
                INT e = -1
                INT f
                INT g
                INT h
                INT i = -1
            ENDSTRUCT

            DATA d

            PROC FOO(DATA &dRef)
            ENDPROC
        ",
        expectedAssembly: @"
            ENTER 0, 2
            STATIC_U8 d
            CALL FOO
            LEAVE 0, 0

        FOO:
            ENTER 1, 3
            LEAVE 1, 0

            .static
        d:  .int 0, -1, -1, -1, -1, 0, 0, 0, -1
        ");
    }

    [Fact]
    public void StructWithArraysDefaultInitializer()
    {
        CompileScript(
        scriptSource: @"
            FOO(d)
        ",
        declarationsSource: @"
            STRUCT DATA
                INT a[12]
                INT b[12]
                INT c[12]
                INT d[9]
                INT e[9]
            ENDSTRUCT

            DATA d

            PROC FOO(DATA &dRef)
            ENDPROC
        ",
        expectedAssembly: @"
            ENTER 0, 2
            STATIC_U8 d
            CALL FOO
            LEAVE 0, 0

        FOO:
            ENTER 1, 3
            LEAVE 1, 0

            .static
        d:  .int 12, 12 dup (0), 12, 12 dup (0), 12, 12 dup (0), 9, 9 dup (0), 9, 9 dup (0)
        ");
    }

    [Fact]
    public void ArrayOfStructsWithDefaultInitializer()
    {
        CompileScript(
        scriptSource: @"
            FOO(d)
        ",
        declarationsSource: @"
            STRUCT DATA
                INT a
                INT b = -1
            ENDSTRUCT

            DATA d[8]

            PROC FOO(DATA dArrayRef[8])
            ENDPROC
        ",
        expectedAssembly: @"
            ENTER 0, 2
            STATIC_U8 d
            CALL FOO
            LEAVE 0, 0

        FOO:
            ENTER 1, 3
            LEAVE 1, 0

            .static
        d:  .int 8, 8 dup (0, -1)
        ");
    }

    [Fact]
    public void StructWithInnerStructWithDefaultInitializer()
    {
        CompileScript(
        scriptSource: @"
            FOO(d)
        ",
        declarationsSource: @"
            STRUCT INNER_INNER_DATA
                INT n[5]
            ENDSTRUCT

            STRUCT INNER_DATA1
                INT n1
                INT n2
                INNER_INNER_DATA n3
            ENDSTRUCT

            STRUCT INNER_DATA2
                INT arr1[4]
                INT arr2[4]
                INT arr3[5]
                INT arr4[4]
                INT n1 = -1
                INT n2 = -1
            ENDSTRUCT

            STRUCT DATA
                INT f_0[4]
                INT f_5, f_6, f_7, f_8, f_9, f_10, f_11, f_12, f_13, f_14, f_15, f_16, f_17, f_18, f_19, f_20
                INT f_21, f_22, f_23, f_24, f_25, f_26, f_27, f_28, f_29, f_30, f_31, f_32, f_33, f_34, f_35, f_36
                INT f_37, f_38, f_39, f_40, f_41, f_42, f_43, f_44, f_45, f_46, f_47, f_48, f_49, f_50, f_51, f_52
                INT f_53, f_54, f_55, f_56, f_57, f_58, f_59, f_60, f_61, f_62
                INNER_DATA1 f_63
                INT f_71, f_72, f_73, f_74, f_75, f_76
                INT f_77 = 2
                INT f_78, f_79, f_80, f_81
                INNER_DATA2 f_82
                INT f_105, f_106, f_107, f_108
                INT f_109[4]
                INT f_114, f_115, f_116, f_117, f_118, f_119, f_120, f_121, f_122, f_123, f_124, f_125, f_126, f_127
                INT f_128, f_129, f_130, f_131, f_132, f_133, f_134, f_135, f_136, f_137, f_138, f_139, f_140
            ENDSTRUCT

            DATA d

            PROC FOO(DATA &dRef)
            ENDPROC
        ",
        expectedAssembly: @"
            ENTER 0, 2
            STATIC_U8 d
            CALL FOO
            LEAVE 0, 0

        FOO:
            ENTER 1, 3
            LEAVE 1, 0

            .static
        d:  .int 4, 4 dup (0)   ; f_0
            .int 60 dup (0)
            .int 5, 5 dup (0)   ; f_63.n3.n
            .int 6 dup (0)
            .int 2              ; f_77
            .int 4 dup (0)
            .int 4, 4 dup (0), 4, 4 dup (0), 5, 5 dup (0), 4, 4 dup (0), -1, -1 ; f_82
            .int 4 dup (0)
            .int 4, 4 dup (0)   ; f_109
            .int 27 dup (0)
        ");
    }

    [Fact]
    public void ScriptParametersAreStoredAsStatics()
    {
        CompileRaw(
        source: @"
                SCRIPT test(VECTOR v, FLOAT a)
                    FLOAT f = a
                ENDSCRIPT
            ",
        expectedAssembly: @"
            .script_name 'test'
            .code
            SCRIPT:
                ENTER 0, 3
                STATIC_U8_LOAD a
                LOCAL_U8_STORE 2
                LEAVE 0, 0

            .arg
            v: .float 0, 0, 0
            a: .float 0
            ");
    }

    [Fact]
    public void ScriptParametersAreStoredAfterStatics()
    {
        CompileRaw(
        source: @"
                FLOAT a
                SCRIPT test(VECTOR v)
                    FLOAT f = v.x
                ENDSCRIPT
            ",
        expectedAssembly: @"
            .script_name 'test'
            .code
            SCRIPT:
                ENTER 0, 3
                STATIC_U8_LOAD v
                LOCAL_U8_STORE 2
                LEAVE 0, 0

            .static
            a: .float 0
            .arg
            v: .float 0, 0, 0
            ");
    }

    [Fact]
    public void ScriptParameterWithDefaultInitializer()
    {
        CompileRaw(
        source: @"
                STRUCT ARGS
                    INT a = 1, b = 2, c = 3
                ENDSTRUCT

                SCRIPT test(ARGS args)
                    INT i = args.a
                ENDSCRIPT
            ",
        expectedAssembly: @"
            .script_name 'test'
            .code
            SCRIPT:
                ENTER 0, 3
                STATIC_U8_LOAD args
                LOCAL_U8_STORE 2
                LEAVE 0, 0

            .arg
            args: .int 1, 2, 3
            ");
    }
}


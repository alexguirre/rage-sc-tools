namespace ScTools.Tests.ScriptLang.CodeGen;

using Xunit;

public class LocalVarsTests : CodeGenTestsBase
{
    [Fact]
    public void LocalsWithoutInitializers()
    {
        CompileScript(
        scriptSource: @"
            INT n1, n2, n3
            VECTOR v
        ",
        expectedAssembly: @"
            ENTER 0, 8
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void IntLocalWithInitializer()
    {
        CompileScript(
        scriptSource: @"
            INT n = 10, m = 300
        ",
        expectedAssembly: @"
            ENTER 0, 4
            PUSH_CONST_U8 10
            LOCAL_U8 2
            STORE
            PUSH_CONST_S16 300
            LOCAL_U8 3
            STORE
            LEAVE 0, 0
        "
        /* optimized expectedAssembly: @"
            ENTER 0, 4
            PUSH_CONST_U8 10
            LOCAL_U8_STORE 2
            PUSH_CONST_S16 300
            LOCAL_U8_STORE 3
            LEAVE 0, 0
        "*/);
    }

    [Fact]
    public void StringLocalsWithInitializer()
    {
        CompileScript(
        scriptSource: @"
            STRING s1 = NULL
            STRING s2 = 'hello world'
            STRING s3 = 'test'
            STRING s4 = 'hello world'
        ",
        expectedAssembly: @"
            ENTER 0, 6
            PUSH_CONST_0
            LOCAL_U8 2
            STORE
            PUSH_CONST_0
            STRING
            LOCAL_U8 3
            STORE
            PUSH_CONST_U8 strTest
            STRING
            LOCAL_U8 4
            STORE
            PUSH_CONST_0
            STRING
            LOCAL_U8 5
            STORE
            LEAVE 0, 0

            .string
            strHelloWorld: .str 'hello world'
            strTest: .str 'test'
        "
        /* optimized expectedAssembly: @"
            ENTER 0, 6
            PUSH_CONST_0
            LOCAL_U8_STORE 2
            PUSH_CONST_0
            STRING
            LOCAL_U8_STORE 3
            PUSH_CONST_U8 strTest
            STRING
            LOCAL_U8_STORE 4
            PUSH_CONST_0
            STRING
            LOCAL_U8_STORE 5
            LEAVE 0, 0

            .string
            strHelloWorld: .str 'hello world'
            strTest: .str 'test'
        "*/);
    }

    [Fact]
    public void StructWithDefaultInitializer()
    {
        CompileScript(
        scriptSource: @"
            DATA d
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
        ",
        expectedAssembly: @"
            ENTER 0, 11
            LOCAL_U8 2

            ; init field b
            DUP
            IOFFSET_U8 1
            PUSH_CONST_M1
            STORE_REV
            DROP

            ; init field c
            DUP
            IOFFSET_U8 2
            PUSH_CONST_M1
            STORE_REV
            DROP

            ; init field d
            DUP
            IOFFSET_U8 3
            PUSH_CONST_M1
            STORE_REV
            DROP

            ; init field e
            DUP
            IOFFSET_U8 4
            PUSH_CONST_M1
            STORE_REV
            DROP

            ; init field i
            DUP
            IOFFSET_U8 8
            PUSH_CONST_M1
            STORE_REV
            DROP

            DROP
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void StructWithArraysDefaultInitializer()
    {
        CompileScript(
        scriptSource: @"
            DATA d
        ",
        declarationsSource: @"
            STRUCT DATA
                INT a[12]
                INT b[12]
                INT c[12]
                INT d[9]
                INT e[9]
            ENDSTRUCT
        ",
        expectedAssembly: @"
            ENTER 0, 61
            LOCAL_U8 2

            ; init field a
            DUP
            PUSH_CONST_U8 12
            STORE_REV
            DROP

            ; init field b
            DUP
            IOFFSET_U8 13
            PUSH_CONST_U8 12
            STORE_REV
            DROP

            ; init field c
            DUP
            IOFFSET_U8 26
            PUSH_CONST_U8 12
            STORE_REV
            DROP

            ; init field d
            DUP
            IOFFSET_U8 39
            PUSH_CONST_U8 9
            STORE_REV
            DROP

            ; init field e
            DUP
            IOFFSET_U8 49
            PUSH_CONST_U8 9
            STORE_REV
            DROP

            DROP
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void ArrayOfStructsWithDefaultInitializer()
    {
        // Test a array of structs with a default initializer found in the game scripts
        // In the b2215 decompiled scripts, it is default initialized as follows:
        //      Var4 = 8;
        //      Var4.f_1.f_1 = -1;
        //      Var4.f_1.f_2.f_1 = -1;
        //      Var4.f_1.f_2.f_2.f_1 = -1;
        //      Var4.f_1.f_2.f_2.f_2.f_1 = -1;
        //      Var4.f_1.f_2.f_2.f_2.f_2.f_1 = -1;
        //      Var4.f_1.f_2.f_2.f_2.f_2.f_2.f_1 = -1;
        //      Var4.f_1.f_2.f_2.f_2.f_2.f_2.f_2.f_1 = -1;
        //      Var4.f_1.f_2.f_2.f_2.f_2.f_2.f_2.f_2.f_1 = -1;
        // 'expectedAssembly' is its corresponding disassembly

        CompileScript(
        scriptSource: @"
            DATA d[8]
        ",
        declarationsSource: @"
            STRUCT DATA
                INT a
                INT b = -1
            ENDSTRUCT
        ",
        expectedAssembly: @"
            ENTER 0, 19
            LOCAL_U8 2
            ; array size
            PUSH_CONST_U8 8
            STORE_REV
            DUP
            IOFFSET_U8 1 ; skip array size

            ; d[0]
            DUP
            IOFFSET_U8 1
            PUSH_CONST_M1
            STORE_REV
            DROP
            IOFFSET_U8 2

            ; d[1]
            DUP
            IOFFSET_U8 1
            PUSH_CONST_M1
            STORE_REV
            DROP
            IOFFSET_U8 2

            ; d[2]
            DUP
            IOFFSET_U8 1
            PUSH_CONST_M1
            STORE_REV
            DROP
            IOFFSET_U8 2

            ; d[3]
            DUP
            IOFFSET_U8 1
            PUSH_CONST_M1
            STORE_REV
            DROP
            IOFFSET_U8 2

            ; d[4]
            DUP
            IOFFSET_U8 1
            PUSH_CONST_M1
            STORE_REV
            DROP
            IOFFSET_U8 2

            ; d[5]
            DUP
            IOFFSET_U8 1
            PUSH_CONST_M1
            STORE_REV
            DROP
            IOFFSET_U8 2

            ; d[6]
            DUP
            IOFFSET_U8 1
            PUSH_CONST_M1
            STORE_REV
            DROP
            IOFFSET_U8 2

            ; d[7]
            DUP
            IOFFSET_U8 1
            PUSH_CONST_M1
            STORE_REV
            DROP
            IOFFSET_U8 2

            DROP
            DROP
            LEAVE 0, 0
        ");
    }

    [Fact]
    public void StructWithInnerStructWithDefaultInitializer()
    {
        // Test a fairly complex struct found in the game scripts
        // In the b2215 decompiled scripts, it is default initialized as follows:
        //      struct<141> Var0;
        //      Var0 = 4;
        //      Var0.f_63.f_2 = 5;
        //      Var0.f_77 = 2;
        //      Var0.f_82 = 4;
        //      Var0.f_82.f_5 = 4;
        //      Var0.f_82.f_10 = 5;
        //      Var0.f_82.f_16 = 4;
        //      Var0.f_82.f_21 = -1;
        //      Var0.f_82.f_22 = -1;
        //      Var0.f_109 = 4;
        // 'expectedAssembly' is its corresponding disassembly

        CompileScript(
        scriptSource: @"
            DATA d
        ",
        declarationsSource: @"
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

            STRUCT INNER_DATA1
                INT n1
                INT n2
                INNER_INNER_DATA n3
            ENDSTRUCT

            STRUCT INNER_INNER_DATA
                INT n[5]
            ENDSTRUCT

            STRUCT INNER_DATA2
                INT arr1[4]
                INT arr2[4]
                INT arr3[5]
                INT arr4[4]
                INT n1 = -1
                INT n2 = -1
            ENDSTRUCT
        ",
        expectedAssembly: @"
            ENTER 0, 143
            LOCAL_U8 2
                ; begin fields of DATA
                ; init d.f_0
                DUP 
                PUSH_CONST_4
                STORE_REV
                DROP

                ; init d.f_63
                DUP
                IOFFSET_U8 63
                    ; init d.f_63.n3
                    DUP
                    IOFFSET_U8 2
                        ; init d.f_63.n3.n
                        DUP
                        PUSH_CONST_5
                        STORE_REV
                        DROP
                    DROP
                DROP

                ; init d.f_77
                DUP
                IOFFSET_U8 77
                PUSH_CONST_2
                STORE_REV
                DROP

                ; init d.f_82
                DUP
                IOFFSET_U8 82
                    ; init d.f_82.arr1
                    DUP
                    PUSH_CONST_4
                    STORE_REV
                    DROP

                    ; init d.f_82.arr2
                    DUP
                    IOFFSET_U8 5
                    PUSH_CONST_4
                    STORE_REV
                    DROP

                    ; init d.f_82.arr3
                    DUP
                    IOFFSET_U8 10
                    PUSH_CONST_5
                    STORE_REV
                    DROP

                    ; init d.f_82.arr4
                    DUP
                    IOFFSET_U8 16
                    PUSH_CONST_4
                    STORE_REV
                    DROP

                    ; init d.f_82.n1
                    DUP
                    IOFFSET_U8 21
                    PUSH_CONST_M1
                    STORE_REV
                    DROP

                    ; init d.f_82.n2
                    DUP
                    IOFFSET_U8 22
                    PUSH_CONST_M1
                    STORE_REV
                    DROP
                DROP

                ; init d.f_109
                DUP
                IOFFSET_U8 109
                PUSH_CONST_4
                STORE_REV
                DROP
                ; end fields of DATA
            DROP
            LEAVE 0, 0
        ");
    }
}


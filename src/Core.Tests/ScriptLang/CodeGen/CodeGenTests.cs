namespace ScTools.Tests.ScriptLang.CodeGen
{
    using System.IO;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang;
    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics;

    using Xunit;

    public class CodeGenTests
    {
        [Fact]
        public void TestLocals()
        {
            CompileMain(
            source: @"
                INT n1, n2, n3
                VECTOR v
            ",
            expectedAssembly: @"
                ENTER 0, 8
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestStatics()
        {
            CompileMain(
            source: @"
            ",
            sourceStatics: @"
                INT n1, n2 = 5, n3
                VECTOR v1, v2 = <<1.0, 2.0, 3.0>>
            ",
            expectedAssembly: @"
                ENTER 0, 2
                LEAVE 0, 0

                .static
                .int 0, 5, 0
                .float 0, 0, 0, 1, 2, 3
            ");
        }

        [Fact]
        public void TestIntLocalWithInitializer()
        {
            CompileMain(
            source: @"
                INT n = 10
            ",
            expectedAssembly: @"
                ENTER 0, 3
                PUSH_CONST_U8 10
                LOCAL_U8_STORE 2
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestStringLocalWithInitializer()
        {
            CompileMain(
            source: @"
                STRING s1 = NULL
                STRING s2 = 'hello world'
                STRING s3 = 'test'
                STRING s4 = 'hello world'
            ",
            expectedAssembly: @"
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
            ");
        }

        [Fact]
        public void TestStructDefaultInitializer()
        {
            CompileMain(
            source: @"
                DATA d
            ",
            sourceStatics: @"
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
        public void TestStructDefaultInitializerStatic()
        {
            CompileMain(
            source: @"",
            sourceStatics: @"
                DATA d

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
                ENTER 0, 2
                LEAVE 0, 0

                .static
                .int 0, -1, -1, -1, -1, 0, 0, 0, -1
            ");
        }

        [Fact]
        public void TestStructWithArraysDefaultInitializer()
        {
            CompileMain(
            source: @"
                DATA d
            ",
            sourceStatics: @"
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
        public void TestStructWithArraysDefaultInitializerStatic()
        {
            CompileMain(
            source: @"",
            sourceStatics: @"
                DATA d

                STRUCT DATA
                    INT a[12]
                    INT b[12]
                    INT c[12]
                    INT d[9]
                    INT e[9]
                ENDSTRUCT
            ",
            expectedAssembly: @"
                ENTER 0, 2
                LEAVE 0, 0

                .static
                .int 12, 12 dup (0), 12, 12 dup (0), 12, 12 dup (0), 9, 9 dup (0), 9, 9 dup (0)
            ");
        }

        [Fact]
        public void TestArrayOfStructsDefaultInitializer()
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

            CompileMain(
            source: @"
                DATA d[8]
            ",
            sourceStatics: @"
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
        public void TestArrayOfStructsDefaultInitializerStatic()
        {
            CompileMain(
            source: @"",
            sourceStatics: @"
                DATA d[8]

                STRUCT DATA
                    INT a
                    INT b = -1
                ENDSTRUCT
            ",
            expectedAssembly: @"
                ENTER 0, 2
                LEAVE 0, 0

                .static
                .int 8, 8 dup (0, -1)
            ");
        }

        [Fact]
        public void TestStructWithInnerStructDefaultInitializer()
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

            CompileMain(
            source: @"
                DATA d
            ",
            sourceStatics: @"
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

        [Fact]
        public void TestStructWithInnerStructDefaultInitializerStatic()
        {
            CompileMain(
            source: @"",
            sourceStatics: @"
                DATA d

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
                ENTER 0, 2
                LEAVE 0, 0

                .static
                .int 4, 4 dup (0)   ; f_0
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
        public void TestVectorOperations()
        {
            CompileMain(
            source: @"
                VECTOR v1 = <<1.0, 2.0, 3.0>>, v2 = <<4.0, 5.0, 6.0>>, v3
                v3 = v1 + v2
                v3 = v1 - v2
                v3 = v1 * v2
                v3 = v1 / v2
                v3 = -v1
            ",
            expectedAssembly: @"
                .const v1 2
                .const v2 5
                .const v3 8
                ENTER 0, 11
                ; v1 = <<1.0, 2.0, 3.0>>
                PUSH_CONST_F1
                PUSH_CONST_F2
                PUSH_CONST_F3
                PUSH_CONST_3
                LOCAL_U8 v1
                STORE_N

                ; v2 = <<4.0, 5.0, 6.0>>
                PUSH_CONST_F4
                PUSH_CONST_F5
                PUSH_CONST_F6
                PUSH_CONST_3
                LOCAL_U8 v2
                STORE_N

                ; v3 = v1 + v2
                PUSH_CONST_3
                LOCAL_U8 v1
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 v2
                LOAD_N
                VADD
                PUSH_CONST_3
                LOCAL_U8 v3
                STORE_N

                ; v3 = v1 - v2
                PUSH_CONST_3
                LOCAL_U8 v1
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 v2
                LOAD_N
                VSUB
                PUSH_CONST_3
                LOCAL_U8 v3
                STORE_N

                ; v3 = v1 * v2
                PUSH_CONST_3
                LOCAL_U8 v1
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 v2
                LOAD_N
                VMUL
                PUSH_CONST_3
                LOCAL_U8 v3
                STORE_N

                ; v3 = v1 / v2
                PUSH_CONST_3
                LOCAL_U8 v1
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 v2
                LOAD_N
                VDIV
                PUSH_CONST_3
                LOCAL_U8 v3
                STORE_N

                ; v3 = -v1
                PUSH_CONST_3
                LOCAL_U8 v1
                LOAD_N
                VNEG
                PUSH_CONST_3
                LOCAL_U8 v3
                STORE_N

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestVectorCompoundAssignments()
        {
            CompileMain(
            source: @"
                VECTOR v1 = <<1.0, 2.0, 3.0>>, v2 = <<4.0, 5.0, 6.0>>
                v1 += v2
                v1 -= v2
                v1 *= v2
                v1 /= v2
            ",
            expectedAssembly: @"
                .const v1 2
                .const v2 5
                ENTER 0, 8
                ; v1 = <<1.0, 2.0, 3.0>>
                PUSH_CONST_F1
                PUSH_CONST_F2
                PUSH_CONST_F3
                PUSH_CONST_3
                LOCAL_U8 v1
                STORE_N

                ; v2 = <<4.0, 5.0, 6.0>>
                PUSH_CONST_F4
                PUSH_CONST_F5
                PUSH_CONST_F6
                PUSH_CONST_3
                LOCAL_U8 v2
                STORE_N

                ; v1 += v2
                PUSH_CONST_3
                LOCAL_U8 v1
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 v2
                LOAD_N
                VADD
                PUSH_CONST_3
                LOCAL_U8 v1
                STORE_N

                ; v1 -= v2
                PUSH_CONST_3
                LOCAL_U8 v1
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 v2
                LOAD_N
                VSUB
                PUSH_CONST_3
                LOCAL_U8 v1
                STORE_N

                ; v1 *= v2
                PUSH_CONST_3
                LOCAL_U8 v1
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 v2
                LOAD_N
                VMUL
                PUSH_CONST_3
                LOCAL_U8 v1
                STORE_N

                ; v1 /= v2
                PUSH_CONST_3
                LOCAL_U8 v1
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 v2
                LOAD_N
                VDIV
                PUSH_CONST_3
                LOCAL_U8 v1
                STORE_N

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIfStatement()
        {
            CompileMain(
            source: @"
                INT n = 5, m = 10
                IF n > m
                    n = 15
                ENDIF
            ",
            expectedAssembly: @"
                ENTER 0, 4
                PUSH_CONST_5
                LOCAL_U8_STORE 2
                PUSH_CONST_U8 10
                LOCAL_U8_STORE 3
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                IGT
                JZ endif
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 2
            endif:
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIfElseStatement()
        {
            CompileMain(
            source: @"
                INT n = 5, m = 10
                IF n <= m
                    n = 15
                ELSE
                    m = 15
                ENDIF
            ",
            expectedAssembly: @"
                ENTER 0, 4
                PUSH_CONST_5
                LOCAL_U8_STORE 2
                PUSH_CONST_U8 10
                LOCAL_U8_STORE 3
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                ILE
                JZ else
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 2
                J endif
            else:
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 3
            endif:
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIfElifStatement()
        {
            CompileMain(
            source: @"
                INT n = 5, m = 10
                IF n > m
                    n = 15
                ELIF n < m
                    m = 15
                ENDIF
            ",
            expectedAssembly: @"
                ENTER 0, 4
                PUSH_CONST_5
                LOCAL_U8_STORE 2
                PUSH_CONST_U8 10
                LOCAL_U8_STORE 3
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                IGT
                JZ elif
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 2
                J endif
            elif:
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                ILT
                JZ endif
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 3
            endif:
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIfElifElseStatement()
        {
            CompileMain(
            source: @"
                INT n = 5, m = 10
                IF n > m
                    n = 15
                ELIF n < m
                    m = 15
                ELSE
                    m = 20
                ENDIF
            ",
            expectedAssembly: @"
                ENTER 0, 4
                PUSH_CONST_5
                LOCAL_U8_STORE 2
                PUSH_CONST_U8 10
                LOCAL_U8_STORE 3
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                IGT
                JZ elif
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 2
                J endif
            elif:
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                ILT
                JZ else
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 3
                J endif
            else:
                PUSH_CONST_U8 20
                LOCAL_U8_STORE 3
            endif:
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestWhileStatement()
        {
            CompileMain(
            source: @"
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
        public void TestRepeatStatement()
        {
            CompileMain(
            source: @"
                INT i
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

        [Fact]
        public void TestNOT()
        {
            CompileMain(
            source: @"
                BOOL b = NOT TRUE
            ",
            expectedAssembly: @"
                ENTER 0, 3
                PUSH_CONST_1
                INOT
                LOCAL_U8_STORE 2
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestShortCircuitAND()
        {
            CompileMain(
            source: @"
                BOOL b = TRUE AND FALSE
            ",
            expectedAssembly: @"
                ENTER 0, 3
                PUSH_CONST_1
                DUP
                JZ assign
                PUSH_CONST_0
                IAND
            assign:
                LOCAL_U8_STORE 2
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestShortCircuitOR()
        {
            CompileMain(
            source: @"
                BOOL b = TRUE OR FALSE
            ",
            expectedAssembly: @"
                ENTER 0, 3
                PUSH_CONST_1
                DUP
                INOT
                JZ assign
                PUSH_CONST_0
                IOR
            assign:
                LOCAL_U8_STORE 2
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestLongShortCircuitAND()
        {
            CompileMain(
            source: @"
                INT n = 1
                IF n == 0 AND n == 3 AND n == 5
                    n = 2
                ENDIF
            ",
            expectedAssembly: @"
                ENTER 0, 3
                PUSH_CONST_1
                LOCAL_U8_STORE 2
                LOCAL_U8_LOAD 2
                PUSH_CONST_0
                IEQ
                DUP
                JZ and
                LOCAL_U8_LOAD 2
                PUSH_CONST_3
                IEQ
                IAND
            and:
                DUP
                JZ if
                LOCAL_U8_LOAD 2
                PUSH_CONST_5
                IEQ
                IAND
            if:
                JZ endif
                PUSH_CONST_2
                LOCAL_U8_STORE 2
            endif:
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestLongShortCircuitOR()
        {
            CompileMain(
            source: @"
                INT n = 1
                IF n == 0 OR n == 3 OR n == 5
                    n = 2
                ENDIF
            ",
            expectedAssembly: @"
                ENTER 0, 3
                PUSH_CONST_1
                LOCAL_U8_STORE 2
                LOCAL_U8_LOAD 2
                PUSH_CONST_0
                IEQ
                DUP
                INOT
                JZ or
                LOCAL_U8_LOAD 2
                PUSH_CONST_3
                IEQ
                IOR
            or:
                DUP
                INOT
                JZ if
                LOCAL_U8_LOAD 2
                PUSH_CONST_5
                IEQ
                IOR
            if:
                JZ endif
                PUSH_CONST_2
                LOCAL_U8_STORE 2
            endif:
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestStructFieldAssignment()
        {
            CompileMain(
            source: @"
                VPAIR pair
                pair.v1 = <<10.0, 20.0, 30.0>>
                pair.v2 = <<0.0, 0.0, 0.0>>
            ",
            sourceStatics: @"
                STRUCT VPAIR
                    VECTOR v1, v2
                ENDSTRUCT
            ",
            expectedAssembly: @"
                ENTER 0, 8

                ; pair.v1 = <<10.0, 20.0, 30.0>>
                PUSH_CONST_F 10.0
                PUSH_CONST_F 20.0
                PUSH_CONST_F 30.0
                PUSH_CONST_3
                LOCAL_U8 2
                STORE_N

                ; pair.v2 = <<0.0, 0.0, 0.0>>
                PUSH_CONST_F0
                PUSH_CONST_F0
                PUSH_CONST_F0
                PUSH_CONST_3
                LOCAL_U8 2
                IOFFSET_U8 3
                STORE_N

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestStructFieldAssignmentSize1()
        {
            CompileMain(
            source: @"
                MY_STRUCT s
                s.a = 1.0
                s.b = 2.0
                s.c = 3.0
            ",
            sourceStatics: @"
                STRUCT MY_STRUCT
                    FLOAT a, b, c
                ENDSTRUCT
            ",
            expectedAssembly: @"
                ENTER 0, 5

                ; s.a = 1.0
                PUSH_CONST_F1
                LOCAL_U8_STORE 2

                ; s.b = 2.0
                PUSH_CONST_F2
                LOCAL_U8 2
                IOFFSET_U8_STORE 1

                ; s.c = 3.0
                PUSH_CONST_F3
                LOCAL_U8 2
                IOFFSET_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestStructFieldAccess()
        {
            CompileMain(
            source: @"
                VPAIR pair
                VECTOR v
                v = pair.v1
                v = pair.v2
            ",
            sourceStatics: @"
                STRUCT VPAIR
                    VECTOR v1, v2
                ENDSTRUCT
            ",
            expectedAssembly: @"
                ENTER 0, 11

                ; v = pair.v1
                PUSH_CONST_3
                LOCAL_U8 2
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 8
                STORE_N

                ; v = pair.v2
                PUSH_CONST_3
                LOCAL_U8 2
                IOFFSET_U8 3
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 8
                STORE_N

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestStructFieldAccessSize1()
        {
            CompileMain(
            source: @"
                MY_STRUCT s
                FLOAT f
                f = s.a
                f = s.b
                f = s.c
            ",
            sourceStatics: @"
                STRUCT MY_STRUCT
                    FLOAT a, b, c
                ENDSTRUCT
            ",
            expectedAssembly: @"
                ENTER 0, 6

                ; f = s.a
                LOCAL_U8_LOAD 2
                LOCAL_U8_STORE 5

                ; f = s.b
                LOCAL_U8 2
                IOFFSET_U8_LOAD 1
                LOCAL_U8_STORE 5

                ; f = s.c
                LOCAL_U8 2
                IOFFSET_U8_LOAD 2
                LOCAL_U8_STORE 5

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestVectorFieldAssignment()
        {
            CompileMain(
            source: @"
                VECTOR v
                v.x = 1.0
                v.y = 2.0
                v.z = 3.0
            ",
            expectedAssembly: @"
                ENTER 0, 5

                ; v.x = 1.0
                PUSH_CONST_F1
                LOCAL_U8_STORE 2

                ; v.y = 2.0
                PUSH_CONST_F2
                LOCAL_U8 2
                IOFFSET_U8_STORE 1

                ; v.z = 3.0
                PUSH_CONST_F3
                LOCAL_U8 2
                IOFFSET_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestVectorFieldAccess()
        {
            CompileMain(
            source: @"
                VECTOR v
                FLOAT f
                f = v.x
                f = v.y
                f = v.z
            ",
            expectedAssembly: @"
                ENTER 0, 6

                ; f = v.x
                LOCAL_U8_LOAD 2
                LOCAL_U8_STORE 5

                ; f = v.y
                LOCAL_U8 2
                IOFFSET_U8_LOAD 1
                LOCAL_U8_STORE 5

                ; f = v.y
                LOCAL_U8 2
                IOFFSET_U8_LOAD 2
                LOCAL_U8_STORE 5

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestArrayItemAssignment()
        {
            CompileMain(
            source: @"
                iNumbers[4] = 123
                iNumbers[5] = 456
            ",
            sourceStatics: @"
                INT iNumbers[8]
            ",
            expectedAssembly: @"
                ENTER 0, 2

                ; iNumbers[4] = 123
                PUSH_CONST_U8 123
                PUSH_CONST_4
                STATIC_U8 iNumbers
                ARRAY_U8_STORE SIZE_OF_INT

                ; iNumbers[5] = 456
                PUSH_CONST_S16 456
                PUSH_CONST_5
                STATIC_U8 iNumbers
                ARRAY_U8_STORE SIZE_OF_INT

                LEAVE 0, 0

                .static
                .const SIZE_OF_INT 1
                iNumbers: .int 8, 8 dup (0)
            ");
        }

        [Fact]
        public void TestArrayItemAccess()
        {
            CompileMain(
            source: @"
                INT n = iNumbers[4]
                n = iNumbers[5]
            ",
            sourceStatics: @"
                INT iNumbers[8]
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n = iNumbers[4]
                PUSH_CONST_4
                STATIC_U8 iNumbers
                ARRAY_U8_LOAD SIZE_OF_INT
                LOCAL_U8_STORE 2

                ; n = iNumbers[5]
                PUSH_CONST_5
                STATIC_U8 iNumbers
                ARRAY_U8_LOAD SIZE_OF_INT
                LOCAL_U8_STORE 2

                LEAVE 0, 0

                .static
                .const SIZE_OF_INT 1
                iNumbers: .int 8, 8 dup (0)
            ");
        }

        [Fact]
        public void TestArrayStructItemAssignment()
        {
            CompileMain(
            source: @"
                vPositions[4] = <<1.0, 2.0, 3.0>>
                vPositions[5] = vPositions[4]
            ",
            sourceStatics: @"
                VECTOR vPositions[8]
            ",
            expectedAssembly: @"
                ENTER 0, 2

                ; vPositions[4] = <<1.0, 2.0, 3.0>>
                PUSH_CONST_F1
                PUSH_CONST_F2
                PUSH_CONST_F3
                PUSH_CONST_3
                PUSH_CONST_4
                STATIC_U8 vPositions
                ARRAY_U8 SIZE_OF_VECTOR
                STORE_N

                ; vPositions[5] = vPositions[4]
                PUSH_CONST_3
                PUSH_CONST_4
                STATIC_U8 vPositions
                ARRAY_U8 SIZE_OF_VECTOR
                LOAD_N
                PUSH_CONST_3
                PUSH_CONST_5
                STATIC_U8 vPositions
                ARRAY_U8 SIZE_OF_VECTOR
                STORE_N

                LEAVE 0, 0

                .static
                .const SIZE_OF_VECTOR 3
                vPositions: .int 8, 8 dup (0, 0, 0)
            ");
        }

        [Fact]
        public void TestArrayStructItemAccess()
        {
            CompileMain(
            source: @"
                VECTOR v = vPositions[4]
                v = vPositions[5]
            ",
            sourceStatics: @"
                VECTOR vPositions[8]
            ",
            expectedAssembly: @"
                ENTER 0, 5

                ; VECTOR v = vPositions[4]
                PUSH_CONST_3
                PUSH_CONST_4
                STATIC_U8 vPositions
                ARRAY_U8 SIZE_OF_VECTOR
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 2
                STORE_N

                ; v = vPositions[5]
                PUSH_CONST_3
                PUSH_CONST_5
                STATIC_U8 vPositions
                ARRAY_U8 SIZE_OF_VECTOR
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 2
                STORE_N

                LEAVE 0, 0

                .static
                .const SIZE_OF_VECTOR 3
                vPositions: .int 8, 8 dup (0, 0, 0)
            ");
        }

        [Fact]
        public void TestArrayItemFieldAssignment()
        {
            CompileMain(
            source: @"
                sDatas[4].a = 123
                sDatas[4].b = 456
            ",
            sourceStatics: @"
                STRUCT DATA
                    INT a, b
                ENDSTRUCT

                DATA sDatas[8]
            ",
            expectedAssembly: @"
                ENTER 0, 2

                ; sDatas[4].a = 123
                PUSH_CONST_U8 123
                PUSH_CONST_4
                STATIC_U8 sDatas
                ARRAY_U8_STORE SIZE_OF_DATA

                ; sDatas[4].b = 456
                PUSH_CONST_S16 456
                PUSH_CONST_4
                STATIC_U8 sDatas
                ARRAY_U8 SIZE_OF_DATA
                IOFFSET_U8_STORE 1

                LEAVE 0, 0

                .static
                .const SIZE_OF_DATA 2
                sDatas: .int 8, 8 dup (0, 0)
            ");
        }

        [Fact]
        public void TestArrayItemFieldAccess()
        {
            CompileMain(
            source: @"
                INT n = sDatas[4].a
                n = sDatas[4].b
            ",
            sourceStatics: @"
                STRUCT DATA
                    INT a, b
                ENDSTRUCT

                DATA sDatas[8]
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n = sDatas[4].a
                PUSH_CONST_4
                STATIC_U8 sDatas
                ARRAY_U8_LOAD SIZE_OF_DATA
                LOCAL_U8_STORE 2

                ; n = sDatas[4].b
                PUSH_CONST_4
                STATIC_U8 sDatas
                ARRAY_U8 SIZE_OF_DATA
                IOFFSET_U8_LOAD 1
                LOCAL_U8_STORE 2

                LEAVE 0, 0

                .static
                .const SIZE_OF_DATA 2
                sDatas: .int 8, 8 dup (0, 0)
            ");
        }

        [Fact]
        public void TestProcedureInvocation()
        {
            CompileMain(
            source: @"
                THE_PROC(1.0, 2.0)
            ",
            sourceStatics: @"
                PROC THE_PROC(FLOAT a, FLOAT b)
                ENDPROC
            ",
            expectedAssembly: @"
                ENTER 0, 2

                ; THE_PROC(1.0, 2.0)
                PUSH_CONST_F1
                PUSH_CONST_F2
                CALL THE_PROC

                LEAVE 0, 0

            THE_PROC:
                ENTER 2, 4
                LEAVE 2, 0
            ");
        }

        [Fact]
        public void TestFunctionInvocation()
        {
            CompileMain(
            source: @"
                INT n = ADD(1234, 5678)
                ADD(1234, 5678)
            ",
            sourceStatics: @"
                FUNC INT ADD(INT a, INT b)
                    RETURN a + b
                ENDFUNC
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n = ADD(1234, 5678)
                PUSH_CONST_S16 1234
                PUSH_CONST_S16 5678
                CALL ADD
                LOCAL_U8_STORE 2

                ; ADD(1234, 5678)
                PUSH_CONST_S16 1234
                PUSH_CONST_S16 5678
                CALL ADD
                DROP

                LEAVE 0, 0

            ADD:
                ENTER 2, 4
                LOCAL_U8_LOAD 0
                LOCAL_U8_LOAD 1
                IADD
                LEAVE 2, 1
            ");
        }

        [Fact]
        public void TestFunctionInvocationWithStructs()
        {
            CompileMain(
            source: @"
                VECTOR v = ADD(<<1.0, 1.0, 1.0>>, <<2.0, 2.0, 2.0>>)
                ADD(<<1.0, 1.0, 1.0>>, <<2.0, 2.0, 2.0>>)
            ",
            sourceStatics: @"
                FUNC VECTOR ADD(VECTOR a, VECTOR b)
                    RETURN a + b
                ENDFUNC
            ",
            expectedAssembly: @"
                ENTER 0, 5

                ; VECTOR v = ADD(<<1.0, 1.0, 1.0>>, <<2.0, 2.0, 2.0>>)
                PUSH_CONST_F1
                PUSH_CONST_F1
                PUSH_CONST_F1
                PUSH_CONST_F2
                PUSH_CONST_F2
                PUSH_CONST_F2
                CALL ADD
                PUSH_CONST_3
                LOCAL_U8 2
                STORE_N

                ; ADD(<<1.0, 1.0, 1.0>>, <<2.0, 2.0, 2.0>>)
                PUSH_CONST_F1
                PUSH_CONST_F1
                PUSH_CONST_F1
                PUSH_CONST_F2
                PUSH_CONST_F2
                PUSH_CONST_F2
                CALL ADD
                DROP
                DROP
                DROP

                LEAVE 0, 0

            ADD:
                ENTER 6, 8
                PUSH_CONST_3
                LOCAL_U8 0
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 3
                LOAD_N
                VADD
                LEAVE 6, 3
            ");
        }

        [Fact]
        public void TestNativeProcedureInvocation()
        {
            // TODO: some better way to define NativeDBs for tests
            var nativeDB = NativeDB.FromJson(@"
            {
                ""TranslationTable"": [[1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317]],
                ""HashToRows"": [{ ""Hash"": 1234605617868164317, ""Rows"": [ 0 ] }],
                ""Commands"": [{
                      ""Hash"": 1234605617868164317,
                      ""Name"": ""TEST"",
                      ""Build"": 323,
                      ""Parameters"": [{ ""Type"": ""int"", ""Name"": ""a""}, { ""Type"": ""int"", ""Name"": ""b""}],
                      ""ReturnType"": ""void""
                    }
                ]
            }");

            CompileMain(
            nativeDB: nativeDB,
            source: @"
                TEST(1234, 5678)
            ",
            sourceStatics: @"
                NATIVE PROC TEST(INT a, INT b)
            ",
            expectedAssembly: @"
                ENTER 0, 2

                ; TEST(1234, 5678)
                PUSH_CONST_S16 1234
                PUSH_CONST_S16 5678
                NATIVE 2, 0, TEST

                LEAVE 0, 0

                .include
                TEST: .native 0x11223344AABBCCDD
            ");
        }

        [Fact]
        public void TestNativeFunctionInvocation()
        {
            var nativeDB = NativeDB.FromJson(@"
            {
                ""TranslationTable"": [[1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317]],
                ""HashToRows"": [{ ""Hash"": 1234605617868164317, ""Rows"": [ 0 ] }],
                ""Commands"": [{
                      ""Hash"": 1234605617868164317,
                      ""Name"": ""TEST"",
                      ""Build"": 323,
                      ""Parameters"": [{ ""Type"": ""int"", ""Name"": ""a""}, { ""Type"": ""int"", ""Name"": ""b""}],
                      ""ReturnType"": ""int""
                    }
                ]
            }");

            CompileMain(
            nativeDB: nativeDB,
            source: @"
                INT n = TEST(1234, 5678)
                TEST(1234, 5678)
            ",
            sourceStatics: @"
                NATIVE FUNC INT TEST(INT a, INT b)
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n = TEST(1234, 5678)
                PUSH_CONST_S16 1234
                PUSH_CONST_S16 5678
                NATIVE 2, 1, TEST
                LOCAL_U8_STORE 2

                ; TEST(1234, 5678)
                PUSH_CONST_S16 1234
                PUSH_CONST_S16 5678
                NATIVE 2, 1, TEST
                DROP

                LEAVE 0, 0

                .include
                TEST: .native 0x11223344AABBCCDD
            ");
        }

        [Fact]
        public void TestFunctionPointerInvocation()
        {
            CompileMain(
            source: @"
                MY_FUNC_T myFunc = ADD
                MY_FUNC_T myFunc2 = myFunc
                INT n = myFunc2(1234, 5678)
                myFunc2(1234, 5678)
            ",
            sourceStatics: @"
                PROTO FUNC INT MY_FUNC_T(INT a, INT b)
                FUNC INT ADD(INT a, INT b)
                    RETURN a + b
                ENDFUNC
            ",
            expectedAssembly: @"
                ENTER 0, 5
                ; MY_FUNC_T myFunc = ADD
                PUSH_CONST_U24 ADD
                LOCAL_U8_STORE 2
                ; MY_FUNC_T myFunc2 = myFunc
                LOCAL_U8_LOAD 2
                LOCAL_U8_STORE 3

                ; INT n = myFunc2(1234, 5678)
                PUSH_CONST_S16 1234
                PUSH_CONST_S16 5678
                LOCAL_U8_LOAD 3
                CALLINDIRECT
                LOCAL_U8_STORE 4

                ; myFunc2(1234, 5678)
                PUSH_CONST_S16 1234
                PUSH_CONST_S16 5678
                LOCAL_U8_LOAD 3
                CALLINDIRECT
                DROP

                LEAVE 0, 0

            ADD:
                ENTER 2, 4
                LOCAL_U8_LOAD 0
                LOCAL_U8_LOAD 1
                IADD
                LEAVE 2, 1
            ");
        }

        [Fact]
        public void TestFunctionPointerComparison()
        {
            CompileMain(
            source: @"
                MY_FUNC_T myFunc
                MY_FUNC_T myFunc2
                BOOL b
                b = myFunc2 == NULL
                b = myFunc2 <> NULL
                b = myFunc2 == myFunc
                b = myFunc2 <> myFunc
                b = myFunc2 == ADD
                b = myFunc2 <> ADD
            ",
            sourceStatics: @"
                PROTO FUNC INT MY_FUNC_T(INT a, INT b)
                FUNC INT ADD(INT a, INT b)
                    RETURN a + b
                ENDFUNC
            ",
            expectedAssembly: @"
                ENTER 0, 5

                ; b = myFunc2 == NULL
                LOCAL_U8_LOAD 3
                PUSH_CONST_0
                IEQ
                LOCAL_U8_STORE 4

                ; b = myFunc2 <> NULL
                LOCAL_U8_LOAD 3
                PUSH_CONST_0
                INE
                LOCAL_U8_STORE 4

                ; b = myFunc2 == myFunc
                LOCAL_U8_LOAD 3
                LOCAL_U8_LOAD 2
                IEQ
                LOCAL_U8_STORE 4

                ; b = myFunc2 <> myFunc
                LOCAL_U8_LOAD 3
                LOCAL_U8_LOAD 2
                INE
                LOCAL_U8_STORE 4

                ; b = myFunc2 == ADD
                LOCAL_U8_LOAD 3
                PUSH_CONST_U24 ADD
                IEQ
                LOCAL_U8_STORE 4

                ; b = myFunc2 <> ADD
                LOCAL_U8_LOAD 3
                PUSH_CONST_U24 ADD
                INE
                LOCAL_U8_STORE 4

                LEAVE 0, 0

            ADD:
                ENTER 2, 4
                LOCAL_U8_LOAD 0
                LOCAL_U8_LOAD 1
                IADD
                LEAVE 2, 1
            ");
        }

        [Fact]
        public void TestTextLabelAsString()
        {
            CompileMain(
            source: @"
                TEXT_LABEL_31 tl31
                STRING s = tl31
                TEST(tl31)
            ",
            sourceStatics: @"
                PROC TEST(STRING s)
                ENDPROC
            ",
            expectedAssembly: @"
                ENTER 0, 7

                ; STRING s = tl31
                LOCAL_U8 2
                LOCAL_U8_STORE 6

                ; TEST(tl31)
                LOCAL_U8 2
                CALL TEST

                LEAVE 0, 0

            TEST:
                ENTER 1, 3
                LEAVE 1, 0
            ");
        }

        [Fact]
        public void TestTextLabelAssignString()
        {
            CompileMain(
            source: @"
                TEXT_LABEL_7 tl7 = 'hello world'
                TEXT_LABEL_15 tl15 = 'hello world'
                TEXT_LABEL_31 tl31 = 'hello world'
                TEXT_LABEL_63 tl63 = 'hello world'
                TEXT_LABEL_127 tl127 = 'hello world'
            ",
            expectedAssembly: @"
                ENTER 0, 33

                ; TEXT_LABEL_7 tl7 = 'hello world'
                PUSH_CONST_0
                STRING
                LOCAL_U8 2
                TEXT_LABEL_ASSIGN_STRING 8

                ; TEXT_LABEL_15 tl15 = 'hello world'
                PUSH_CONST_0
                STRING
                LOCAL_U8 3
                TEXT_LABEL_ASSIGN_STRING 16

                ; TEXT_LABEL_31 tl31 = 'hello world'
                PUSH_CONST_0
                STRING
                LOCAL_U8 5
                TEXT_LABEL_ASSIGN_STRING 32

                ; TEXT_LABEL_63 tl63 = 'hello world'
                PUSH_CONST_0
                STRING
                LOCAL_U8 9
                TEXT_LABEL_ASSIGN_STRING 64

                ; TEXT_LABEL_127 tl127 = 'hello world'
                PUSH_CONST_0
                STRING
                LOCAL_U8 17
                TEXT_LABEL_ASSIGN_STRING 128

                LEAVE 0, 0

                .string
                .str 'hello world'
            ");
        }

        [Fact]
        public void TestTextLabelAssignInt()
        {
            CompileMain(
            source: @"
                TEXT_LABEL_7 tl7 = 123456
                TEXT_LABEL_15 tl15 = 123456
                TEXT_LABEL_31 tl31 = 123456
                TEXT_LABEL_63 tl63 = 123456
                TEXT_LABEL_127 tl127 = 123456
            ",
            expectedAssembly: @"
                ENTER 0, 33

                ; TEXT_LABEL_7 tl7 = 123456
                PUSH_CONST_U24 123456
                LOCAL_U8 2
                TEXT_LABEL_ASSIGN_INT 8

                ; TEXT_LABEL_15 tl15 = 123456
                PUSH_CONST_U24 123456
                LOCAL_U8 3
                TEXT_LABEL_ASSIGN_INT 16

                ; TEXT_LABEL_31 tl31 = 123456
                PUSH_CONST_U24 123456
                LOCAL_U8 5
                TEXT_LABEL_ASSIGN_INT 32

                ; TEXT_LABEL_63 tl63 = 123456
                PUSH_CONST_U24 123456
                LOCAL_U8 9
                TEXT_LABEL_ASSIGN_INT 64

                ; TEXT_LABEL_127 tl127 = 123456
                PUSH_CONST_U24 123456
                LOCAL_U8 17
                TEXT_LABEL_ASSIGN_INT 128

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestTextLabelAssignTextLabel()
        {
            CompileMain(
            source: @"
                TEXT_LABEL_31 src
                TEXT_LABEL_7 tl7 = src
                TEXT_LABEL_15 tl15 = src
                TEXT_LABEL_31 tl31 = src
                TEXT_LABEL_63 tl63 = src
                TEXT_LABEL_127 tl127 = src
            ",
            expectedAssembly: @"
                ENTER 0, 37

                ; TEXT_LABEL_7 tl7 = src
                PUSH_CONST_4
                LOCAL_U8 2
                LOAD_N
                PUSH_CONST_4
                PUSH_CONST_1
                LOCAL_U8 6
                TEXT_LABEL_COPY

                ; TEXT_LABEL_15 tl15 = src
                PUSH_CONST_4
                LOCAL_U8 2
                LOAD_N
                PUSH_CONST_4
                PUSH_CONST_2
                LOCAL_U8 7
                TEXT_LABEL_COPY

                ; TEXT_LABEL_31 tl31 = src
                PUSH_CONST_4
                LOCAL_U8 2
                LOAD_N
                PUSH_CONST_4
                LOCAL_U8 9
                STORE_N

                ; TEXT_LABEL_63 tl63 = src
                PUSH_CONST_4
                LOCAL_U8 2
                LOAD_N
                PUSH_CONST_4
                PUSH_CONST_U8 8
                LOCAL_U8 13
                TEXT_LABEL_COPY

                ; TEXT_LABEL_127 tl127 = src
                PUSH_CONST_4
                LOCAL_U8 2
                LOAD_N
                PUSH_CONST_4
                PUSH_CONST_U8 16
                LOCAL_U8 21
                TEXT_LABEL_COPY

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestTextLabelAppendString()
        {
            CompileMain(
            source: @"
                TEXT_LABEL_7 tl7
                TEXT_LABEL_15 tl15
                TEXT_LABEL_31 tl31
                TEXT_LABEL_63 tl63
                TEXT_LABEL_127 tl127

                APPEND(tl7, 'hello world')
                APPEND(tl15, 'hello world')
                APPEND(tl31, 'hello world')
                APPEND(tl63, 'hello world')
                APPEND(tl127, 'hello world')
            ",
            expectedAssembly: @"
                ENTER 0, 33

                ; APPEND(tl7, 'hello world')
                PUSH_CONST_0
                STRING
                LOCAL_U8 2
                TEXT_LABEL_APPEND_STRING 8

                ; APPEND(tl15, 'hello world')
                PUSH_CONST_0
                STRING
                LOCAL_U8 3
                TEXT_LABEL_APPEND_STRING 16

                ; APPEND(tl31, 'hello world')
                PUSH_CONST_0
                STRING
                LOCAL_U8 5
                TEXT_LABEL_APPEND_STRING 32

                ; APPEND(tl63, 'hello world')
                PUSH_CONST_0
                STRING
                LOCAL_U8 9
                TEXT_LABEL_APPEND_STRING 64

                ; APPEND(tl127, 'hello world')
                PUSH_CONST_0
                STRING
                LOCAL_U8 17
                TEXT_LABEL_APPEND_STRING 128

                LEAVE 0, 0

                .string
                .str 'hello world'
            ");
        }

        [Fact]
        public void TestTextLabelAppendInt()
        {
            CompileMain(
            source: @"
                TEXT_LABEL_7 tl7
                TEXT_LABEL_15 tl15
                TEXT_LABEL_31 tl31
                TEXT_LABEL_63 tl63
                TEXT_LABEL_127 tl127

                APPEND(tl7, 123456)
                APPEND(tl15, 123456)
                APPEND(tl31, 123456)
                APPEND(tl63, 123456)
                APPEND(tl127, 123456)
            ",
            expectedAssembly: @"
                ENTER 0, 33

                ; APPEND(tl7, 123456)
                PUSH_CONST_U24 123456
                LOCAL_U8 2
                TEXT_LABEL_APPEND_INT 8

                ; APPEND(tl15, 123456)
                PUSH_CONST_U24 123456
                LOCAL_U8 3
                TEXT_LABEL_APPEND_INT 16

                ; APPEND(tl31, 123456)
                PUSH_CONST_U24 123456
                LOCAL_U8 5
                TEXT_LABEL_APPEND_INT 32

                ; APPEND(tl63, 123456)
                PUSH_CONST_U24 123456
                LOCAL_U8 9
                TEXT_LABEL_APPEND_INT 64

                ; APPEND(tl127, 123456)
                PUSH_CONST_U24 123456
                LOCAL_U8 17
                TEXT_LABEL_APPEND_INT 128

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestTextLabelAppendTextLabel()
        {
            // supported due to TEXT_LABEL to STRING implicit conversion
            CompileMain(
            source: @"
                TEXT_LABEL_31 src
                TEXT_LABEL_7 tl7
                TEXT_LABEL_15 tl15
                TEXT_LABEL_31 tl31
                TEXT_LABEL_63 tl63
                TEXT_LABEL_127 tl127

                APPEND(tl7, src)
                APPEND(tl15, src)
                APPEND(tl31, src)
                APPEND(tl63, src)
                APPEND(tl127, src)
            ",
            expectedAssembly: @"
                ENTER 0, 37

                ; APPEND(tl7, src)
                LOCAL_U8 2
                LOCAL_U8 6
                TEXT_LABEL_APPEND_STRING 8

                ; APPEND(tl15, src)
                LOCAL_U8 2
                LOCAL_U8 7
                TEXT_LABEL_APPEND_STRING 16

                ; APPEND(tl31, src)
                LOCAL_U8 2
                LOCAL_U8 9
                TEXT_LABEL_APPEND_STRING 32

                ; APPEND(tl63, src)
                LOCAL_U8 2
                LOCAL_U8 13
                TEXT_LABEL_APPEND_STRING 64

                ; APPEND(tl127, src)
                LOCAL_U8 2
                LOCAL_U8 21
                TEXT_LABEL_APPEND_STRING 128

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestTextLabelAsParameter()
        {
            CompileMain(
            source: @"
                TEXT_LABEL_31 tl
                TEST(tl)
            ",
            sourceStatics: @"
                PROC TEST(TEXT_LABEL_31 tl)
                    APPEND(tl, 1234)
                ENDPROC
            ",
            expectedAssembly: @"
                ENTER 0, 6

                ; TEST(tl)
                PUSH_CONST_4
                LOCAL_U8 2
                LOAD_N
                CALL TEST

                LEAVE 0, 0

            TEST:
                ENTER 4, 6

                ; APPEND(tl, 1234)
                PUSH_CONST_S16 1234
                LOCAL_U8 0
                TEXT_LABEL_APPEND_INT 32

                LEAVE 4, 0
            ");
        }

        [Fact]
        public void TestReferences()
        {
            CompileMain(
            source: @"
                FLOAT f
                TEST(f, f)
            ",
            sourceStatics: @"
                PROC TEST(FLOAT& a, FLOAT b)
                    b = a
                    a = b
                    TEST(a, b)
                ENDPROC
            ",
            expectedAssembly: @"
                ENTER 0, 3
                LOCAL_U8 2
                LOCAL_U8_LOAD 2
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 2, 4

                ; b = a
                LOCAL_U8_LOAD 0
                LOAD
                LOCAL_U8_STORE 1

                ; a = b
                LOCAL_U8_LOAD 1
                LOCAL_U8_LOAD 0
                STORE

                ; TEST(a, b)
                LOCAL_U8_LOAD 0
                LOCAL_U8_LOAD 1
                CALL TEST
                LEAVE 2, 0
            ");
        }

        [Fact]
        public void TestStructReferences()
        {
            CompileMain(
            source: @"
                VECTOR v
                TEST(v, v)
            ",
            sourceStatics: @"
                PROC TEST(VECTOR& a, VECTOR b)
                    b = a
                    a = b
                    TEST(a, b)
                ENDPROC
            ",
            expectedAssembly: @"
                ENTER 0, 5
                LOCAL_U8 2
                PUSH_CONST_3
                LOCAL_U8 2
                LOAD_N
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 4, 6

                ; b = a
                PUSH_CONST_3
                LOCAL_U8_LOAD 0
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 1
                STORE_N

                ; a = b
                PUSH_CONST_3
                LOCAL_U8 1
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8_LOAD 0
                STORE_N

                ; TEST(a, b)
                LOCAL_U8_LOAD 0
                PUSH_CONST_3
                LOCAL_U8 1
                LOAD_N
                CALL TEST
                LEAVE 4, 0
            ");
        }

        [Fact]
        public void TestArrayReferences()
        {
            CompileMain(
            source: @"
                INT v[10]
                TEST(v, v)
            ",
            sourceStatics: @"
                PROC TEST(INT a[10], INT b[]) // arrays are passed by reference
                    a[1] = b[1]
                    b[1] = a[1]
                    TEST(a, b)
                ENDPROC
            ",
            expectedAssembly: @"
                ENTER 0, 13

                ; array initializer
                LOCAL_U8 2
                PUSH_CONST_U8 10
                STORE_REV
                DROP

                LOCAL_U8 2
                LOCAL_U8 2
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 2, 4

                ; a[1] = b[1]
                PUSH_CONST_1
                LOCAL_U8_LOAD 1
                ARRAY_U8_LOAD 1
                PUSH_CONST_1
                LOCAL_U8_LOAD 0
                ARRAY_U8_STORE 1

                ; b[1] = a[1]
                PUSH_CONST_1
                LOCAL_U8_LOAD 0
                ARRAY_U8_LOAD 1
                PUSH_CONST_1
                LOCAL_U8_LOAD 1
                ARRAY_U8_STORE 1

                ; TEST(a, b)
                LOCAL_U8_LOAD 0
                LOCAL_U8_LOAD 1
                CALL TEST

                LEAVE 2, 0
            ");
        }

        [Fact]
        public void TestConstants()
        {
            CompileMain(
            source: @"
                INT i = MY_INT
                FLOAT f = MY_FLOAT
                BOOL b = MY_BOOL
                STRING s = MY_STRING
            ",
            sourceStatics: @"
                CONST INT MY_INT = 1234
                CONST FLOAT MY_FLOAT = 12.34
                CONST BOOL MY_BOOL = TRUE
                CONST STRING MY_STRING = 'hello'
            ",
            expectedAssembly: @"
                ENTER 0, 6

                ; INT i = MY_INT
                PUSH_CONST_S16 1234
                LOCAL_U8_STORE 2

                ; FLOAT f = MY_FLOAT
                PUSH_CONST_F 12.34
                LOCAL_U8_STORE 3

                ; BOOL b = MY_BOOL
                PUSH_CONST_1
                LOCAL_U8_STORE 4

                ; STRING s = MY_STRING
                PUSH_CONST_0
                STRING
                LOCAL_U8_STORE 5

                LEAVE 0, 0

                .string
                .str 'hello'
            ");
        }

        [Fact]
        public void TestEnums()
        {
            CompileMain(
            source: @"
                MY_ENUM e
                e = MY_ENUM_A
                e = MY_ENUM_B
                e = MY_ENUM_C
                e = MY_ENUM_D
            ",
            sourceStatics: @"
                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B
                    MY_ENUM_C = -10
                    MY_ENUM_D
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; e = MY_ENUM_A
                PUSH_CONST_0
                LOCAL_U8_STORE 2

                ; e = MY_ENUM_B
                PUSH_CONST_1
                LOCAL_U8_STORE 2

                ; e = MY_ENUM_C
                PUSH_CONST_S16 -10
                LOCAL_U8_STORE 2

                ; e = MY_ENUM_D
                PUSH_CONST_S16 -9
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestEnumsWithInitializers()
        {
            CompileMain(
            source: @"
                MY_ENUM e
                e = MY_ENUM_A
                e = MY_ENUM_B
                e = MY_ENUM_C
                e = MY_ENUM_D
                e = MY_ENUM_E
                e = MY_ENUM_F
            ",
            sourceStatics: @"
                ENUM MY_ENUM
                    MY_ENUM_A
                    MY_ENUM_B = ENUM_TO_INT(MY_ENUM_A) + 1
                    MY_ENUM_C = ENUM_TO_INT(MY_ENUM_B) + 1
                    MY_ENUM_D
                    MY_ENUM_E = -10
                    MY_ENUM_F = MY_ENUM_E
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; e = MY_ENUM_A
                PUSH_CONST_0
                LOCAL_U8_STORE 2

                ; e = MY_ENUM_B
                PUSH_CONST_1
                LOCAL_U8_STORE 2

                ; e = MY_ENUM_C
                PUSH_CONST_2
                LOCAL_U8_STORE 2

                ; e = MY_ENUM_D
                PUSH_CONST_3
                LOCAL_U8_STORE 2

                ; e = MY_ENUM_E
                PUSH_CONST_S16 -10
                LOCAL_U8_STORE 2

                ; e = MY_ENUM_F
                PUSH_CONST_S16 -10
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIntrinsicF2V()
        {
            CompileMain(
            source: @"
                VECTOR v = F2V(1.0)
            ",
            expectedAssembly: @"
                ENTER 0, 5

                PUSH_CONST_F1
                F2V
                PUSH_CONST_3
                LOCAL_U8 2
                STORE_N

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIntrinsicF2I()
        {
            CompileMain(
            source: @"
                INT i = F2I(1.0)
            ",
            expectedAssembly: @"
                ENTER 0, 3

                PUSH_CONST_F1
                F2I
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIntrinsicI2F()
        {
            CompileMain(
            source: @"
                FLOAT f = I2F(1)
            ",
            expectedAssembly: @"
                ENTER 0, 3

                PUSH_CONST_1
                I2F
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIntrinsicCountOf()
        {
            CompileMain(
            source: @"
                INT array[10]
                INT i = COUNT_OF(array)
                TEST1(array)
                TEST2(array)
            ",
            sourceStatics: @"
                PROC TEST1(INT array[])
                    INT i = COUNT_OF(array)
                ENDPROC
                PROC TEST2(INT array[10])
                    INT i = COUNT_OF(array)
                ENDPROC
            ",
            expectedAssembly: @"
                ENTER 0, 14
                ; array initializer
                LOCAL_U8 2
                PUSH_CONST_U8 10
                STORE_REV
                DROP

                ; INT i = COUNT_OF(array)
                PUSH_CONST_U8 10
                LOCAL_U8_STORE 13

                LOCAL_U8 2
                CALL TEST1

                LOCAL_U8 2
                CALL TEST2
                LEAVE 0, 0

            TEST1:
                ENTER 1, 4

                ; INT i = COUNT_OF(array)
                LOCAL_U8_LOAD 0
                LOAD
                LOCAL_U8_STORE 3

                LEAVE 1, 0

            TEST2:
                ENTER 1, 4

                ; INT i = COUNT_OF(array)
                PUSH_CONST_U8 10
                LOCAL_U8_STORE 3

                LEAVE 1, 0
            ");
        }

        [Fact(Skip = "Optimization not implemented yet")]
        public void TestPushU8Optimization()
        {
            CompileMain(
            source: @"
                TEST(8, 11 + 1)
            ",
            sourceStatics: @"
                PROC TEST(INT a, INT b)
                ENDPROC
            ",
            expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_U8_U8 8, 11
                IADD_U8 1
                CALL TEST
                LEAVE 0, 0

            TEST:
                ENTER 2, 4
                LEAVE 2, 0
            ");
        }

        [Fact]
        public void TestScriptParameters()
        {
            CompileRaw(
            source: @"
                SCRIPT test(VECTOR v, FLOAT a)
                    FLOAT f = v.x + v.y + v.z + a
                ENDSCRIPT
            ",
            expectedAssembly: @"
            .script_name test
            .code
            SCRIPT:
                ENTER 0, 3
                STATIC_U8_LOAD v
                STATIC_U8 v
                IOFFSET_U8_LOAD 1
                FADD
                STATIC_U8 v
                IOFFSET_U8_LOAD 2
                FADD
                STATIC_U8_LOAD a
                FADD
                LOCAL_U8_STORE 2
                LEAVE 0, 0

            .arg
            v: .float 0, 0, 0
            a: .float 0
            ");
        }

        [Fact]
        public void TestScriptParameterWithDefaultInitializer()
        {
            CompileRaw(
            source: @"
                STRUCT ARGS
                    INT a = 1, b = 2, c = 3
                ENDSTRUCT

                SCRIPT test(ARGS args)
                    INT i = args.a + args.b + args.c
                ENDSCRIPT
            ",
            expectedAssembly: @"
            .script_name test
            .code
            SCRIPT:
                ENTER 0, 3
                STATIC_U8_LOAD args
                STATIC_U8 args
                IOFFSET_U8_LOAD 1
                IADD
                STATIC_U8 args
                IOFFSET_U8_LOAD 2
                IADD
                LOCAL_U8_STORE 2
                LEAVE 0, 0

            .arg
            args: .int 1, 2, 3
            ");
        }

        [Fact]
        public void TestConversionToEntityIndex()
        {
            CompileMain(
            source: @"
                ENTITY_INDEX entity
                PED_INDEX ped
                VEHICLE_INDEX vehicle
                OBJECT_INDEX object

                entity = ped
                entity = vehicle
                entity = object

                TEST(ped, vehicle, object)
            ",
            sourceStatics: @"
                PROC TEST(ENTITY_INDEX e1, ENTITY_INDEX e2, ENTITY_INDEX e3)
                ENDPROC
            ",
            expectedAssembly: @"
                ENTER 0, 6
                
                ; entity = ped
                LOCAL_U8_LOAD 3
                LOCAL_U8_STORE 2
                
                ; entity = vehicle
                LOCAL_U8_LOAD 4
                LOCAL_U8_STORE 2
                
                ; entity = object
                LOCAL_U8_LOAD 5
                LOCAL_U8_STORE 2

                ; TEST(ped, vehicle, object)
                LOCAL_U8_LOAD 3
                LOCAL_U8_LOAD 4
                LOCAL_U8_LOAD 5
                CALL TEST

                LEAVE 0, 0

            TEST:
                ENTER 3, 5
                LEAVE 3, 0
            ");
        }

        [Fact]
        public void TestHandleTypeAssignNull()
        {
            CompileMain(
            source: @"
                ENTITY_INDEX entity = NULL
                PED_INDEX ped = NULL
                VEHICLE_INDEX vehicle = NULL
                OBJECT_INDEX object = NULL
            ",
            expectedAssembly: @"
                ENTER 0, 6

                ; entity = NULL
                PUSH_CONST_0
                LOCAL_U8_STORE 2

                ; ped = NULL
                PUSH_CONST_0
                LOCAL_U8_STORE 3

                ; vehicle = NULL
                PUSH_CONST_0
                LOCAL_U8_STORE 4

                ; object = NULL
                PUSH_CONST_0
                LOCAL_U8_STORE 5

                LEAVE 0, 0
            ");
        }

        [Theory]
        [InlineData("==", "IEQ")]
        [InlineData("<>", "INE")]
        public void TestHandleTypeComparison(string comparisonOperator, string opcode)
        {
            CompileMain(
            source: $@"
                ENTITY_INDEX entity
                PED_INDEX ped
                VEHICLE_INDEX vehicle
                OBJECT_INDEX object

                BOOL b
                b = entity {comparisonOperator} NULL
                b = entity {comparisonOperator} entity
                b = ped {comparisonOperator} entity
                b = ped {comparisonOperator} NULL
                b = ped {comparisonOperator} ped
                b = vehicle {comparisonOperator} entity
                b = vehicle {comparisonOperator} NULL
                b = vehicle {comparisonOperator} vehicle
                b = object {comparisonOperator} entity
                b = object {comparisonOperator} NULL
                b = object {comparisonOperator} object
            ",
            expectedAssembly: $@"
                ENTER 0, 7

                ; b = entity op NULL
                LOCAL_U8_LOAD 2
                PUSH_CONST_0
                {opcode}
                LOCAL_U8_STORE 6

                ; b = entity op entity
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 2
                {opcode}
                LOCAL_U8_STORE 6

                ; b = ped op entity
                LOCAL_U8_LOAD 3
                LOCAL_U8_LOAD 2
                {opcode}
                LOCAL_U8_STORE 6

                ; b = ped op NULL
                LOCAL_U8_LOAD 3
                PUSH_CONST_0
                {opcode}
                LOCAL_U8_STORE 6

                ; b = ped op ped
                LOCAL_U8_LOAD 3
                LOCAL_U8_LOAD 3
                {opcode}
                LOCAL_U8_STORE 6

                ; b = vehicle op entity
                LOCAL_U8_LOAD 4
                LOCAL_U8_LOAD 2
                {opcode}
                LOCAL_U8_STORE 6

                ; b = vehicle op NULL
                LOCAL_U8_LOAD 4
                PUSH_CONST_0
                {opcode}
                LOCAL_U8_STORE 6

                ; b = vehicle op vehicle
                LOCAL_U8_LOAD 4
                LOCAL_U8_LOAD 4
                {opcode}
                LOCAL_U8_STORE 6

                ; b = object op entity
                LOCAL_U8_LOAD 5
                LOCAL_U8_LOAD 2
                {opcode}
                LOCAL_U8_STORE 6

                ; b = object op NULL
                LOCAL_U8_LOAD 5
                PUSH_CONST_0
                {opcode}
                LOCAL_U8_STORE 6

                ; b = object op object
                LOCAL_U8_LOAD 5
                LOCAL_U8_LOAD 5
                {opcode}
                LOCAL_U8_STORE 6

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestEnumCountOf()
        {
            CompileMain(
            source: @"
                INT n1 = COUNT_OF(MY_ENUM)
            ",
            sourceStatics: @"
                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, MY_ENUM_C
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n1 = COUNT_OF(MY_ENUM)
                PUSH_CONST_3
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestEnumToInt()
        {
            CompileMain(
            source: @"
                INT n1 = ENUM_TO_INT(MY_ENUM_B)
            ",
            sourceStatics: @"
                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, MY_ENUM_C
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n1 = ENUM_TO_INT(MY_ENUM_B)
                PUSH_CONST_1
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIntToEnum()
        {
            CompileMain(
            source: @"
                INT n1 = ENUM_TO_INT(MY_ENUM_B)
                MY_ENUM e1 = INT_TO_ENUM(MY_ENUM, n1)
                MY_ENUM e2 = INT_TO_ENUM(MY_ENUM, 1)
            ",
            sourceStatics: @"
                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, MY_ENUM_C
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 5

                ; INT n1 = ENUM_TO_INT(MY_ENUM_B)
                PUSH_CONST_1
                LOCAL_U8_STORE 2

                ; MY_ENUM e1 = INT_TO_ENUM(MY_ENUM, n1)
                LOCAL_U8_LOAD 2
                LOCAL_U8_STORE 3

                ; MY_ENUM e2 = INT_TO_ENUM(MY_ENUM, 1)
                PUSH_CONST_1
                LOCAL_U8_STORE 4

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestArraySizeWithEnumToInt()
        {
            CompileMain(
            source: @"
            ",
            sourceStatics: @"
                INT iNumbers[ENUM_TO_INT(NUM_MY_ENUM)]

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, NUM_MY_ENUM
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 2
                LEAVE 0, 0

                .static
                .int 2, 2 dup (0)
            ");
        }

        [Fact]
        public void TestArraySizeWithEnumToIntAndIntToEnum()
        {
            CompileMain(
            source: @"
            ",
            sourceStatics: @"
                INT iNumbers[ENUM_TO_INT(INT_TO_ENUM(MY_ENUM, ENUM_TO_INT(NUM_MY_ENUM)))]

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, NUM_MY_ENUM
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 2
                LEAVE 0, 0

                .static
                .int 2, 2 dup (0)
            ");
        }

        [Fact]
        public void TestArraySizeWithCountOf()
        {
            CompileMain(
            source: @"
            ",
            sourceStatics: @"
                INT iNumbers[COUNT_OF(MY_ENUM)]

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, MY_ENUM_C
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 2
                LEAVE 0, 0

                .static
                .int 3, 3 dup (0)
            ");
        }

        [Fact]
        public void TestIntConstantWithEnumToInt()
        {
            CompileMain(
            source: @"
                INT n = NUM_MY_ENUM_INT
            ",
            sourceStatics: @"
                CONST INT NUM_MY_ENUM_INT = ENUM_TO_INT(NUM_MY_ENUM)

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, NUM_MY_ENUM
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n = NUM_MY_ENUM_INT
                PUSH_CONST_2
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIntConstantWithCountOf()
        {
            CompileMain(
            source: @"
                INT n = MY_NUMBER
            ",
            sourceStatics: @"
                CONST INT MY_NUMBER = COUNT_OF(MY_ENUM)

                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, MY_ENUM_C
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n = MY_NUMBER
                PUSH_CONST_3
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIntConstantWithF2I()
        {
            CompileMain(
            source: @"
                INT n = MY_INT
            ",
            sourceStatics: @"
                CONST INT MY_INT = F2I(1.8)
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n = MY_INT
                PUSH_CONST_1
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestFloatConstantWithI2F()
        {
            CompileMain(
            source: @"
                FLOAT f = MY_FLOAT
            ",
            sourceStatics: @"
                CONST FLOAT MY_FLOAT = I2F(2)
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; FLOAT f = MY_FLOAT
                PUSH_CONST_F2
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIntConstantWithF2IAndI2F()
        {
            CompileMain(
            source: @"
                INT n = MY_INT
            ",
            sourceStatics: @"
                CONST INT MY_INT = F2I(I2F(2))
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n = MY_INT
                PUSH_CONST_2
                LOCAL_U8_STORE 2

                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestSwitchCaseWithInt()
        {
            CompileMain(
            source: @"
                SWITCH 5
                    CASE 1
                        BREAK
                    CASE 2
                        BREAK
                    CASE 3
                        BREAK
                    DEFAULT
                        BREAK
                ENDSWITCH
            ",
            expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_5
                SWITCH 1:case1, 2:case2, 3:case3
                J default
            case1:   J endswitch
            case2:   J endswitch
            case3:   J endswitch
            default: J endswitch
            endswitch:
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestSwitchCaseWithIntAndHashes()
        {
            CompileMain(
            source: @"
                SWITCH 5
                    CASE 1
                        BREAK
                    CASE 2
                        BREAK
                    CASE `hello`
                        BREAK
                    CASE `world`
                        BREAK
                    DEFAULT
                        BREAK
                ENDSWITCH
            ",
            expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_5
                SWITCH 1:case1, 2:case2, 0xC8FD181B:caseHello, 0x9099ABE4:caseWorld
                J default
            case1:      J endswitch
            case2:      J endswitch
            caseHello:  J endswitch
            caseWorld:  J endswitch
            default:    J endswitch
            endswitch:
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestSwitchCaseWithEnum()
        {
            CompileMain(
            source: @"
                SWITCH MY_ENUM_B
                    CASE MY_ENUM_A
                        BREAK
                    CASE MY_ENUM_B
                        BREAK
                    CASE INT_TO_ENUM(MY_ENUM, ENUM_TO_INT(MY_ENUM_B) + 1)
                        BREAK
                    CASE INT_TO_ENUM(MY_ENUM, -1)
                        BREAK
                    DEFAULT
                        BREAK
                ENDSWITCH
            ",
            sourceStatics: @"
                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B
                ENDENUM
            ",
            expectedAssembly: @"
                ENTER 0, 2
                PUSH_CONST_1
                SWITCH 0:case0, 1:case1, 2:case2, 0xFFFFFFFF:caseM1
                J default
            case0:   J endswitch
            case1:   J endswitch
            case2:   J endswitch
            caseM1:  J endswitch
            default: J endswitch
            endswitch:
                LEAVE 0, 0
            ");
        }

        private static void CompileMain(string source, string expectedAssembly, string sourceStatics = "", NativeDB? nativeDB = null)
            => CompileRaw(
                source: $@"
                    {sourceStatics}
                    SCRIPT test_script
                    {source}
                    ENDSCRIPT",

                expectedAssembly: $@"
                    .script_name test_script
                    .code
                    SCRIPT:
                    {expectedAssembly}",
                
                nativeDB: nativeDB);

        private static void CompileRaw(string source, string expectedAssembly, NativeDB? nativeDB = null)
        {
            using var sourceReader = new StringReader(source);
            var d = new DiagnosticsReport();
            var p = new Parser(sourceReader, "test.sc");
            p.Parse(d);

            var globalSymbols = GlobalSymbolTableBuilder.Build(p.OutputAst, d);
            IdentificationVisitor.Visit(p.OutputAst, d, globalSymbols);
            TypeChecker.Check(p.OutputAst, d, globalSymbols);

            Assert.False(d.HasErrors);

            using var sink = new StringWriter();
            new CodeGenerator(sink, p.OutputAst, globalSymbols, d, nativeDB ?? NativeDB.Empty).Generate();
            var s = sink.ToString();

            Assert.False(d.HasErrors);

            using var sourceAssemblyReader = new StringReader(s);
            var sourceAssembler = Assembler.Assemble(sourceAssemblyReader, "test.scasm", nativeDB ?? NativeDB.Empty, options: new() { IncludeFunctionNames = true });

            Assert.False(sourceAssembler.Diagnostics.HasErrors);

            using var expectedAssemblyReader = new StringReader(expectedAssembly);
            var expectedAssembler = Assembler.Assemble(expectedAssemblyReader, "test_expected.scasm", nativeDB ?? NativeDB.Empty, options: new() { IncludeFunctionNames = true });

            using StringWriter sourceDumpWriter = new(), expectedDumpWriter = new();
            new Dumper(sourceAssembler.OutputScript).Dump(sourceDumpWriter, true, true, true, true, true);
            new Dumper(expectedAssembler.OutputScript).Dump(expectedDumpWriter, true, true, true, true, true);

            string sourceDump = sourceDumpWriter.ToString(), expectedDump = expectedDumpWriter.ToString();

            Util.AssertScriptsAreEqual(sourceAssembler.OutputScript, expectedAssembler.OutputScript);
        }
    }
}

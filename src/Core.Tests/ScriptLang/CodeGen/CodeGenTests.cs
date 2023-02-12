namespace ScTools.Tests.ScriptLang.CodeGen
{
    using System.IO;
    using System.Linq;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Targets.Five;
    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Workspace;
    //using ScTools.ScriptLang.Semantics;

    using Xunit;

    public class CodeGenTests
    {
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
                IGT_JZ endif
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 2
            endif:
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIfStatement2()
        {
            CompileMain(
            source: @"
                FLOAT n = 5.0, m = 10.0
                IF n > m
                    n = 15.0
                ENDIF
            ",
            expectedAssembly: @"
                ENTER 0, 4
                PUSH_CONST_F5
                LOCAL_U8_STORE 2
                PUSH_CONST_F 10.0
                LOCAL_U8_STORE 3
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                FGT
                JZ endif
                PUSH_CONST_F 15.0
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
                ILE_JZ else
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
        public void TestIfElseStatement2()
        {
            CompileMain(
            source: @"
                FLOAT n = 5.0, m = 10.0
                IF n <= m
                    n = 15.0
                ELSE
                    m = 15.0
                ENDIF
            ",
            expectedAssembly: @"
                ENTER 0, 4
                PUSH_CONST_F5
                LOCAL_U8_STORE 2
                PUSH_CONST_F 10.0
                LOCAL_U8_STORE 3
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                FLE
                JZ else
                PUSH_CONST_F 15.0
                LOCAL_U8_STORE 2
                J endif
            else:
                PUSH_CONST_F 15.0
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
                IGT_JZ elif
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 2
                J endif
            elif:
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                ILT_JZ endif
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 3
            endif:
                LEAVE 0, 0
            ");
        }

        [Fact]
        public void TestIfElifStatement2()
        {
            CompileMain(
            source: @"
                FLOAT n = 5.0, m = 10.0
                IF n > m
                    n = 15.0
                ELIF n < m
                    m = 15.0
                ENDIF
            ",
            expectedAssembly: @"
                ENTER 0, 4
                PUSH_CONST_F5
                LOCAL_U8_STORE 2
                PUSH_CONST_F 10.0
                LOCAL_U8_STORE 3
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                FGT
                JZ elif
                PUSH_CONST_F 15.0
                LOCAL_U8_STORE 2
                J endif
            elif:
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                FLT
                JZ endif
                PUSH_CONST_F 15.0
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
                IGT_JZ elif
                PUSH_CONST_U8 15
                LOCAL_U8_STORE 2
                J endif
            elif:
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                ILT_JZ else
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
        public void TestIfElifElseStatement2()
        {
            CompileMain(
            source: @"
                FLOAT n = 5.0, m = 10.0
                IF n > m
                    n = 15.0
                ELIF n < m
                    m = 15.0
                ELSE
                    m = 20.0
                ENDIF
            ",
            expectedAssembly: @"
                ENTER 0, 4
                PUSH_CONST_F5
                LOCAL_U8_STORE 2
                PUSH_CONST_F 10.0
                LOCAL_U8_STORE 3
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                FGT
                JZ elif
                PUSH_CONST_F 15.0
                LOCAL_U8_STORE 2
                J endif
            elif:
                LOCAL_U8_LOAD 2
                LOCAL_U8_LOAD 3
                FLT
                JZ else
                PUSH_CONST_F 15.0
                LOCAL_U8_STORE 3
                J endif
            else:
                PUSH_CONST_F 20.0
                LOCAL_U8_STORE 3
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
        public void TestBigStructFieldAssignment()
        {
            CompileMain(
            source: @"
                pair.v1 = <<10.0, 20.0, 30.0>>
                pair.v2 = <<0.0, 0.0, 0.0>>
            ",
            sourceStatics: @"
                VPAIR pair
                STRUCT VPAIR
                    INT padding[99999]
                    VECTOR v1, v2
                ENDSTRUCT
            ",
            expectedAssembly: @"
                ENTER 0, 2

                ; pair.v1 = <<10.0, 20.0, 30.0>>
                PUSH_CONST_F 10.0
                PUSH_CONST_F 20.0
                PUSH_CONST_F 30.0
                PUSH_CONST_3
                STATIC_U8 0
                PUSH_CONST_U24 100000
                IOFFSET
                STORE_N

                ; pair.v2 = <<0.0, 0.0, 0.0>>
                PUSH_CONST_F0
                PUSH_CONST_F0
                PUSH_CONST_F0
                PUSH_CONST_3
                STATIC_U8 0
                PUSH_CONST_U24 100003
                IOFFSET
                STORE_N

                LEAVE 0, 0

                .static
                .int 99999, 99999 dup (0)
                .float 6 dup (0.0)
            ");
        }

        [Fact]
        public void TestBigStructFieldAccess()
        {
            CompileMain(
            source: @"
                VECTOR v
                v = pair.v1
                v = pair.v2
            ",
            sourceStatics: @"
                VPAIR pair
                STRUCT VPAIR
                    INT padding[99999]
                    VECTOR v1, v2
                ENDSTRUCT
            ",
            expectedAssembly: @"
                ENTER 0, 5

                ; v = pair.v1
                PUSH_CONST_3
                STATIC_U8 0
                PUSH_CONST_U24 100000
                IOFFSET
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 2
                STORE_N

                ; v = pair.v2
                PUSH_CONST_3
                STATIC_U8 0
                PUSH_CONST_U24 100003
                IOFFSET
                LOAD_N
                PUSH_CONST_3
                LOCAL_U8 2
                STORE_N

                LEAVE 0, 0

                .static
                .int 99999, 99999 dup (0)
                .float 6 dup (0.0)
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
                TEST(tl31)
            ",
            sourceStatics: @"
                PROC TEST(STRING s)
                ENDPROC
            ",
            expectedAssembly: @"
                ENTER 0, 6

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
                    TEXT_LABEL_APPEND_INT(tl, 1234)
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
            ",
            sourceStatics: @"
                CONST_INT MY_INT 1234
                CONST_FLOAT MY_FLOAT 12.34
            ",
            expectedAssembly: @"
                ENTER 0, 4

                ; INT i = MY_INT
                PUSH_CONST_S16 1234
                LOCAL_U8_STORE 2

                ; FLOAT f = MY_FLOAT
                PUSH_CONST_F 12.34
                LOCAL_U8_STORE 3

                LEAVE 0, 0
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
                ENUM MY_ENUM
                    MY_ENUM_A, MY_ENUM_B, NUM_MY_ENUM
                ENDENUM

                CONST_INT NUM_MY_ENUM_INT ENUM_TO_INT(NUM_MY_ENUM)
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
                INT arr[3]
                CONST_INT MY_NUMBER COUNT_OF(arr)
            ",
            expectedAssembly: @"
                ENTER 0, 3

                ; INT n = MY_NUMBER
                PUSH_CONST_3
                LOCAL_U8_STORE 2

                LEAVE 0, 0

                .static
                .int 3, 0, 0, 0
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
                CONST_INT MY_INT F2I(1.8)
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
                CONST_FLOAT MY_FLOAT I2F(2)
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
                CONST_INT MY_INT F2I(I2F(2))
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
                    .script_name 'test_script'
                    .code
                    SCRIPT:
                    {expectedAssembly}",
                
                nativeDB: nativeDB);

        private static void CompileRaw(string source, string expectedAssembly, NativeDB? nativeDB = null)
        {
            nativeDB ??= NativeDB.Empty;

            var d = new DiagnosticsReport();
            var l = new Lexer("codegen_tests.sc", source, d);
            var p = new Parser(l, d);
            var s = new SemanticsAnalyzer(d);

            var u = p.ParseCompilationUnit();
            Assert.False(d.HasErrors);
            u.Accept(s);
            Assert.False(d.HasErrors);

            var compiledScripts = ScriptCompiler.Compile(u, new(Game.GTAV, Platform.x64));
            Assert.False(d.HasErrors);
            var compiledScript = Assert.Single(compiledScripts);
            var compiledScriptGTAV = IsType<GameFiles.Five.Script>(compiledScript);

            using var expectedAssemblyReader = new StringReader(expectedAssembly);
            var expectedAssembler = Assembler.Assemble(expectedAssemblyReader, "test_expected.scasm", nativeDB, options: new() { IncludeFunctionNames = true });

            string sourceDump = new DumperFiveV12().DumpToString(compiledScriptGTAV);
            string expectedDump = new DumperFiveV12().DumpToString(expectedAssembler.OutputScript);

            Util.AssertScriptsAreEqual(compiledScriptGTAV, expectedAssembler.OutputScript);
        }
    }
}

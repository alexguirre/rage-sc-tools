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

        [Fact(Skip = "Ref assignment not yet implemented")]
        public void TestRefAssignment()
        {
            CompileMain(
            source: @"
                FLOAT f
                FLOAT& fref = f
                fref = 1.0
            ",
            expectedAssembly: @"
                ENTER 0, 4
                LOCAL_U8 2
                LOCAL_U8_STORE 3
                PUSH_CONST_F1
                LOCAL_U8_LOAD 3
                STORE
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

        private static void CompileMain(string source, string expectedAssembly, string sourceStatics = "")
        {
            using var sourceReader = new StringReader($@"
                SCRIPT_NAME test
                {sourceStatics}
                PROC MAIN()
                {source}
                ENDPROC");
            var d = new DiagnosticsReport();
            var p = new Parser(sourceReader, "test.sc");
            p.Parse(d);

            var globalSymbols = GlobalSymbolTableBuilder.Build(p.OutputAst, d);
            IdentificationVisitor.Visit(p.OutputAst, d, globalSymbols);
            TypeChecker.Check(p.OutputAst, d, globalSymbols);

            Assert.False(d.HasErrors);

            using var sink = new StringWriter();
            new CodeGenerator(sink, p.OutputAst, globalSymbols, d, NativeDB.Empty).Generate();
            var s = sink.ToString();

            Assert.False(d.HasErrors);

            using var sourceAssemblyReader = new StringReader(s);
            var sourceAssembler = Assembler.Assemble(sourceAssemblyReader, "test.scasm", NativeDB.Empty, options: new() { IncludeFunctionNames = true });

            Assert.False(sourceAssembler.Diagnostics.HasErrors);

            using var expectedAssemblyReader = new StringReader($@"
                .script_name test
                .code
                MAIN:
                {expectedAssembly}");
            var expectedAssembler = Assembler.Assemble(expectedAssemblyReader, "test_expected.scasm", NativeDB.Empty, options: new() { IncludeFunctionNames = true });

            using StringWriter sourceDumpWriter = new(), expectedDumpWriter = new();
            new Dumper(sourceAssembler.OutputScript).Dump(sourceDumpWriter, true, true, true, true, true);
            new Dumper(expectedAssembler.OutputScript).Dump(expectedDumpWriter, true, true, true, true, true);

            string sourceDump = sourceDumpWriter.ToString(), expectedDump = expectedDumpWriter.ToString();

            Util.AssertScriptsAreEqual(sourceAssembler.OutputScript, expectedAssembler.OutputScript);
        }
    }
}

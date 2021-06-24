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

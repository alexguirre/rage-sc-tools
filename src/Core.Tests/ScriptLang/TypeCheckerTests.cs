namespace ScTools.Tests.ScriptLang
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Semantics;

    using Xunit;

    public class TypeCheckerTests
    {
        [Fact]
        public void TestAssignIntToTextLabel()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_31 tl = 1234
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestAssignStringToTextLabel()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_31 tl = 'hello'
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestAssignTextLabelToTextLabelOfGreaterSize()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_15 tl1 = 'hello'
                    TEXT_LABEL_31 tl2 = tl1
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestAssignTextLabelToTextLabelOfSmallerSize()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_31 tl1 = 'hello'
                    TEXT_LABEL_15 tl2 = tl1
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestAssignTextLabelToTextLabelOfSameSize()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_15 tl1 = 'hello'
                    TEXT_LABEL_15 tl2 = tl1
                ENDSCRIPT
            ");

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestCannotPassIntToTextLabelParameter()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEST(1234)
                ENDSCRIPT

                PROC TEST(TEXT_LABEL_31 tl)
                ENDPROC
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestCannotPassStringToTextLabelParameter()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEST('hello')
                ENDSCRIPT

                PROC TEST(TEXT_LABEL_31 tl)
                ENDPROC
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestCannotPassTextLabelOfDifferentSizeToTextLabelParameter()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_15 tl
                    TEST(tl)
                ENDSCRIPT

                PROC TEST(TEXT_LABEL_31 tl)
                ENDPROC
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestPassTextLabelOfSameSizeToTextLabelParameter()
        {
            var d = TypeCheck($@"
                SCRIPT test_script
                    TEXT_LABEL_31 tl
                    TEST(tl)
                ENDSCRIPT

                PROC TEST(TEXT_LABEL_31 tl)
                ENDPROC
            ");

            Assert.False(d.HasErrors);
        }

        private static DiagnosticsReport TypeCheck(string source)
        {
            using var sourceReader = new StringReader(source);
            var d = new DiagnosticsReport();
            var p = new Parser(sourceReader, "test.sc");
            p.Parse(d);

            var globalSymbols = GlobalSymbolTableBuilder.Build(p.OutputAst, d);
            IdentificationVisitor.Visit(p.OutputAst, d, globalSymbols);
            TypeChecker.Check(p.OutputAst, d, globalSymbols);

            return d;
        }
    }
}

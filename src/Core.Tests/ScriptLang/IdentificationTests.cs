namespace ScTools.Tests.ScriptLang
{
    using System.IO;
    using System.Linq;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Semantics;

    using Xunit;

    public class IdentificationTests
    {
        [Fact]
        public void TestMissingMain()
        {
            var d = ParseAndIdentify($@"");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestIncorrectMain1()
        {
            var d = ParseAndIdentify($@"
                FUNC INT MAIN()
                ENDFUNC
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestIncorrectMain2()
        {
            var d = ParseAndIdentify($@"
                PROC MAIN(INT n)
                ENDPROC
            ");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        private static DiagnosticsReport ParseAndIdentify(string source)
        {
            using var sourceReader = new StringReader($@"
                SCRIPT_NAME test
                {source}");
            var d = new DiagnosticsReport();
            var p = new Parser(sourceReader, "test.sc");
            p.Parse(d);

            var globalSymbols = GlobalSymbolTableBuilder.Build(p.OutputAst, d);
            IdentificationVisitor.Visit(p.OutputAst, d, globalSymbols);
            return d;
        }
    }
}

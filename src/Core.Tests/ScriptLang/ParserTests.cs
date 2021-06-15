namespace ScTools.Tests.ScriptLang
{
    using System.IO;
    using System.Linq;

    using ScTools.ScriptLang;

    using Xunit;

    public class ParserTests
    {
        [Fact]
        public void TestInt()
        {
            Assert.Equal(123456,            P("123456"));
            Assert.Equal(0x1234ABCD,        P("0x1234ABCD"));
            Assert.Equal(H("hello"),        P("`hello`"));
            Assert.Equal(H("hello\nworld"), P("`hello\\nworld`"));
            Assert.Equal(H("hello\tworld"), P("`hello\\tworld`"));

            static int H(string s) => unchecked((int)Util.CalculateHash(s));
            static int P(string s)
            {
                var d = new DiagnosticsReport();
                using var r = new StringReader(@$"
                    SCRIPT_HASH {s}
                ");
                var parser = new Parser(r, "test.sc");
                parser.Parse(d);
                Assert.False(d.HasErrors || d.HasWarnings);
                return parser.OutputAst.ScriptHash;
            }
        }

        //[Fact]
        //public void TestString()
        //{
        //    var d = new Diagnostics();
        //    using var r = new StringReader(@"
        //        USING 'hello'
        //        USING ""hello""
        //        USING '\thello""world'
        //        USING '\thello\'world'
        //        USING ""\thello\""world""
        //        USING ""\thello'world""
        //    ");
        //    var parser = new Parser(r, "test.sc");
        //    parser.Parse(d);

        //    var ast = parser.OutputAst;
        //    var u = ast.Directives.Cast<UsingDirective>().ToArray();

        //    Assert.Equal("hello",           u[0].Path);
        //    Assert.Equal("hello",           u[1].Path);
        //    Assert.Equal("\thello\"world",  u[2].Path);
        //    Assert.Equal("\thello'world",   u[3].Path);
        //    Assert.Equal("\thello\"world",  u[4].Path);
        //    Assert.Equal("\thello'world",   u[5].Path);

        //    static int H(string s) => unchecked((int)Util.CalculateHash(s));
        //}
    }
}

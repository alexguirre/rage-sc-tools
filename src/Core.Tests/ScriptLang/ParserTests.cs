namespace ScTools.Tests.ScriptLang
{
    using System.IO;
    using System.Linq;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast.Directives;

    using Xunit;

    public class ParserTests
    {
        [Fact]
        public void TestInt()
        {
            var d = new Diagnostics();
            using var r = new StringReader(@"
                SCRIPT_HASH 123456
                SCRIPT_HASH 0x1234ABCD
                SCRIPT_HASH `hello`
                SCRIPT_HASH `hello\nworld`
                SCRIPT_HASH `hello\tworld`
            ");
            var parser = new Parser(r, "test.sc");
            parser.Parse(d);

            var ast = parser.OutputAst;
            var h = ast.Directives.Cast<ScriptHashDirective>().ToArray();

            Assert.Equal(123456,            h[0].Hash);
            Assert.Equal(0x1234ABCD,        h[1].Hash);
            Assert.Equal(H("hello"),        h[2].Hash);
            Assert.Equal(H("hello\nworld"), h[3].Hash);
            Assert.Equal(H("hello\tworld"), h[4].Hash);

            static int H(string s) => unchecked((int)Util.CalculateHash(s));
        }
        [Fact]
        public void TestString()
        {
            var d = new Diagnostics();
            using var r = new StringReader(@"
                USING 'hello'
                USING ""hello""
                USING '\thello""world'
                USING '\thello\'world'
                USING ""\thello\""world""
                USING ""\thello'world""
            ");
            var parser = new Parser(r, "test.sc");
            parser.Parse(d);

            var ast = parser.OutputAst;
            var u = ast.Directives.Cast<UsingDirective>().ToArray();

            Assert.Equal("hello",           u[0].Path);
            Assert.Equal("hello",           u[1].Path);
            Assert.Equal("\thello\"world",  u[2].Path);
            Assert.Equal("\thello'world",   u[3].Path);
            Assert.Equal("\thello\"world",  u[4].Path);
            Assert.Equal("\thello'world",   u[5].Path);

            static int H(string s) => unchecked((int)Util.CalculateHash(s));
        }
    }
}

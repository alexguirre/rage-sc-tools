namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;

    using Xunit;

    public class StringTests
    {
        [Fact]
        public void TestSingleAndDoubleQuotes()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    STRING a = ""Hello""
                    STRING b = 'World'
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestStaticStringWithInitializer()
        {
            var module = Util.ParseAndAnalyze($@"
                STRING a = 'Hello'

                PROC MAIN()
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors, "Static string cannot be initialized");
        }

        [Fact]
        public void TestStaticString()
        {
            var module = Util.ParseAndAnalyze($@"
                STRING a

                PROC MAIN()
                    a = 'Hello'
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }
    }
}

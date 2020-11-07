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
            var module = Module.Compile(new StringReader($@"
                PROC MAIN()
                    STRING a = ""Hello""
                    STRING b = 'World'
                ENDPROC
            "));

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestStaticStringWithInitializer()
        {
            var module = Module.Compile(new StringReader($@"
                STRING a = 'Hello'

                PROC MAIN()
                ENDPROC
            "));

            Assert.True(module.Diagnostics.HasErrors, "Static string cannot be initialized");
        }

        [Fact]
        public void TestStaticString()
        {
            var module = Module.Compile(new StringReader($@"
                STRING a

                PROC MAIN()
                    a = 'Hello'
                ENDPROC
            "));

            Assert.False(module.Diagnostics.HasErrors);
        }
    }
}

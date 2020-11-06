namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;

    using Xunit;

    public class TypeCheckTests
    {
        [Theory]
        [InlineData("BOOL v = 4")]
        [InlineData("INT v = TRUE")]
        [InlineData("FLOAT v = 2")]
        [InlineData("INT v = <<1, 2, 3>>")]
        [InlineData("VEC3 v = 4.0")]
        [InlineData("VEC3 v = <<1.0, 2.0>>")]
        public void TestStaticInitializersIncorrectTypes(string staticDecl)
        {
            var module = Module.Parse(new StringReader($@"
                {staticDecl}

                PROC MAIN()
                ENDPROC
            "));

            Assert.True(module.Diagnostics.HasErrors, $"Expected errors due to incorrect type in static initializer '{staticDecl}'");
        }

        [Theory]
        [InlineData("BOOL v = TRUE")]
        [InlineData("BOOL v = FALSE")]
        [InlineData("BOOL v = 3 == 6")]
        [InlineData("INT v = 3")]
        [InlineData("FLOAT v = 1.0")]
        [InlineData("FLOAT v = 2.0 + 3.0")]
        [InlineData("VEC3 v = <<1.0, 2.0, 3.0>>")]
        public void TestStaticInitializersCorrectTypes(string staticDecl)
        {
            var module = Module.Parse(new StringReader($@"
                {staticDecl}

                PROC MAIN()
                ENDPROC
            "));

            Assert.False(module.Diagnostics.HasErrors, $"Expected no errors due to incorrect type in static initializer '{staticDecl}'");
        }
    }
}

namespace ScTools.Tests.ScriptLang
{
    using Xunit;

    public class MultipleDeclsTests
    {
        [Fact]
        public void TestStatics()
        {
            var c = Util.Compile($@"
                INT a = 5, b = 10, c[2], d

                PROC MAIN()
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
            Assert.NotNull(c.CompiledScript);
            Assert.Equal(5,  c.CompiledScript.Statics[0].AsInt32);
            Assert.Equal(10, c.CompiledScript.Statics[1].AsInt32);
            Assert.Equal(2,  c.CompiledScript.Statics[2].AsInt32);
            Assert.Equal(0,  c.CompiledScript.Statics[5].AsInt32);
        }

        [Fact]
        public void TestConsts()
        {
            var c = Util.Compile($@"
                CONST INT A = 5 + C, B = 10 + A, C = 2

                INT n = A + B + C

                PROC MAIN()
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
            Assert.NotNull(c.CompiledScript);
            Assert.Equal(7 + 17 + 2, c.CompiledScript.Statics[0].AsInt32);
        }

        [Fact]
        public void TestLocals()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT a = 5, b = 10 + a, c[2], d, &e = b
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestLocalsUseBeforeDecl()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT a = 5 + b, b = 10
                ENDPROC
            ");

            Assert.True(c.GetAllDiagnostics().HasErrors);
        }
    }
}

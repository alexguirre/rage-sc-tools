namespace ScTools.Tests.ScriptLang
{
    using Xunit;

    public class IntrinsicsTests
    {
        [Fact]
        public void TestExpressions()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT a = 5
                    FLOAT b = I2F(a)
                    INT c = F2I(b)
                    VECTOR d = F2V(I2F(c))
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestProcedures()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT a = 5
                    I2F(a)
                    F2V(I2F(a))

                    TEXT_LABEL32 label
                    ASSIGN_STRING(label, 'HELLO')
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }
    }
}

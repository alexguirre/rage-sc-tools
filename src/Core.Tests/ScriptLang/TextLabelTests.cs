namespace ScTools.Tests.ScriptLang
{
    using Xunit;

    public class TextLabelTests
    {
        [Fact]
        public void TestLocal()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    TEXT_LABEL32 lbl
                    ASSIGN_STRING(lbl, 'hello')
                    APPEND_STRING(lbl, 'world')
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestStatic()
        {
            var c = Util.Compile($@"
                TEXT_LABEL32 lbl

                PROC MAIN()
                    ASSIGN_STRING(lbl, 'hello')
                    APPEND_STRING(lbl, 'world')
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
            var s = Util.Dump(c.CompiledScript);
            ;
        }

        [Fact]
        public void TestInts()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT n = 5, m = 10
                    TEXT_LABEL16 lbl
                    ASSIGN_INT(lbl, n)
                    APPEND_INT(lbl, m)
                    APPEND_INT(lbl, n)
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestCopy()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    TEXT_LABEL64 a, b, c

                    ASSIGN_STRING(a, 'hello world')
                    ASSIGN_STRING(b, 'other string')
                    c = a
                    //APPEND_STRING(c, b)
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }
    }
}

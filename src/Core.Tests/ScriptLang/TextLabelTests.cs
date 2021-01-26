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
                    APPEND_STRING(c, b)
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestConversionToStringInAssignment()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    TEXT_LABEL64 a
                    ASSIGN_STRING(a, 'hello world')

                    STRING s1 = a
                    s1 = a
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestConversionToStringInRefAssignment()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    TEXT_LABEL64 a
                    ASSIGN_STRING(a, 'hello world')

                    STRING s1
                    STRING& s1Ref = s1
                    s1Ref = a
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestConversionToStringInInvocation()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    TEXT_LABEL64 a
                    ASSIGN_STRING(a, 'hello world')

                    SOMETHING(a)
                    INT n = SOMETHING_ELSE(a)
                ENDPROC

                PROC SOMETHING(STRING str)
                ENDPROC

                FUNC INT SOMETHING_ELSE(STRING str)
                    RETURN 1
                ENDFUNC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestConversionToStringInReturn()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    STRING s = GET_STRING()
                ENDPROC

                TEXT_LABEL64 a
                FUNC STRING GET_STRING()
                    ASSIGN_STRING(a, 'hello world')
                    RETURN a
                ENDFUNC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestConversionToStringInAssignIntrinsic()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    TEXT_LABEL64 a, b
                    ASSIGN_STRING(a, 'hello world')
                    ASSIGN_STRING(b, a)
                    APPEND_STRING(b, a)
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }
    }
}

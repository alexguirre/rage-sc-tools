namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;

    using Xunit;

    public class IndirectCallTests
    {
        [Fact]
        public void TestProc()
        {
            var c = Util.Compile($@"
                PROTO PROC MY_PROCEDURE_PROTOTYPE()

                PROC MAIN()
                    MY_PROCEDURE_PROTOTYPE myProc = MY_PROCEDURE
                    myProc()
                ENDPROC

                PROC MY_PROCEDURE()
                    INT a = 5
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestFunc()
        {
            var c = Util.Compile($@"
                PROTO FUNC INT MY_FUNCTION_PROTOTYPE()

                PROC MAIN()
                    MY_FUNCTION_PROTOTYPE myFunc = MY_FUNCTION
                    myFunc()
                    INT n = myFunc()
                ENDPROC

                FUNC INT MY_FUNCTION()
                    RETURN 5
                ENDFUNC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestStatic()
        {
            var c = Util.Compile($@"
                PROTO PROC MY_PROCEDURE_PROTOTYPE()

                MY_PROCEDURE_PROTOTYPE myProc

                PROC MAIN()
                    myProc = MY_PROCEDURE
                    myProc()
                ENDPROC

                PROC MY_PROCEDURE()
                    INT a = 5
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestStaticInitializer()
        {
            var c = Util.Compile($@"
                PROTO PROC MY_PROCEDURE_PROTOTYPE()

                MY_PROCEDURE_PROTOTYPE myProc = MY_PROCEDURE

                PROC MAIN()
                    myProc()
                ENDPROC

                PROC MY_PROCEDURE()
                    INT a = 5
                ENDPROC
            ");

            Assert.True(c.GetAllDiagnostics().HasErrors, "Cannot statically initialize procedure/function references");
        }
    }
}

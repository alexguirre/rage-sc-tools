namespace ScTools.Tests.ScriptLang
{
    using Xunit;

    public class RepeatTests
    {
        [Fact]
        public void Test()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT n = 1

                    INT i
                    REPEAT n i
                        DO_SOMETHING(i)
                    ENDREPEAT

                    REPEAT 5 i
                        DO_SOMETHING(i)
                    ENDREPEAT
                ENDPROC

                PROC DO_SOMETHING(INT x)
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }
    }
}

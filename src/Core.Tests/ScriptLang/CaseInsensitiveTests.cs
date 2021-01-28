namespace ScTools.Tests.ScriptLang
{
    using System.Linq;

    using Xunit;

    public class CaseInsensitiveTests
    {
        [Fact]
        public void Test()
        {
            // note: function names are uppercase in all because they get embedded in the bytecode, so they cannot be different
            var c = Util.Compile($@"
                CONST INT SOME_CONSTANT = 5

                PROC MAIN()
                    INT someVariable = SOME_CONSTANT
                    INT otherVariable = someVariable + SOME_CONSTANT
                    DO_SOMETHING()

                    IF someVariable == 1
                    ENDIF

                    WHILE someVariable > 1
                    ENDWHILE
                ENDPROC

                PROC DO_SOMETHING()
                ENDPROC
            ");
            Assert.False(c.GetAllDiagnostics().HasErrors);

            var cLowercase = Util.Compile($@"
                const int SOME_CONSTANT = 5

                proc MAIN()
                    int someVariable = some_constant
                    int otherVariable = somevariable + some_constant
                    do_something()

                    if somevariable == 1
                    endif

                    while somevariable > 1
                    endwhile
                endproc

                proc DO_SOMETHING()
                endproc
            ");
            Assert.False(cLowercase.GetAllDiagnostics().HasErrors);

            var cMixed = Util.Compile($@"
                Const Int SOME_CONSTANT = 5

                Proc MAIN()
                    Int someVariable = Some_Constant
                    Int otherVariable = SomeVariable + Some_Constant
                    Do_Something()

                    If SomeVariable == 1
                    EndIf

                    While SomeVariable > 1
                    EndWhile
                EndProc

                proc DO_SOMETHING()
                endproc
            ");
            Assert.False(cMixed.GetAllDiagnostics().HasErrors);

            // check that all cases produce the same bytecode
            foreach (var ((@base, lowercase), mixed) in c.CompiledScript.CodePages.Items.Select(p => p.Data)
                                                    .Zip(cLowercase.CompiledScript.CodePages.Items.Select(p => p.Data))
                                                    .Zip(cMixed.CompiledScript.CodePages.Items.Select(p => p.Data)))
            {
                Assert.Equal(@base, lowercase);
                Assert.Equal(@base, mixed);
            }
        }

        [Fact]
        public void TestMain()
        {
            var c1 = Util.Compile($@"
                proc main()
                endproc
            ");
            Assert.False(c1.GetAllDiagnostics().HasErrors);

            var c2 = Util.Compile($@"
                Proc Main()
                EndProc
            ");
            Assert.False(c2.GetAllDiagnostics().HasErrors);

            var c3 = Util.Compile($@"
                PrOc MaIn()
                EnDPrOc
            ");
            Assert.False(c3.GetAllDiagnostics().HasErrors);
        }
    }
}

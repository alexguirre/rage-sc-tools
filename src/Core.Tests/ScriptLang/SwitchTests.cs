namespace ScTools.Tests.ScriptLang
{
    using Xunit;

    public class SwitchTests
    {
        [Fact]
        public void Test()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT n = 1

                    SWITCH n
                        CASE 0
                            n = 10

                        CASE 1
                            n = 20

                        CASE 2
                            n = 30

                        DEFAULT
                            n = 40
                    ENDSWITCH
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }


        [Fact]
        public void TestNested()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT n = 0
                    INT m = 2

                    SWITCH n
                        CASE 0
                            SWITCH m
                                CASE 1
                                    n = 10

                                CASE 2
                                    n = 100
                            ENDSWITCH
                        CASE 1
                            n = 20
                        CASE 2
                            n = 30
                        CASE 2 * 2
                            n = 40
                        DEFAULT
                            n = 50
                    ENDSWITCH
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestConstants()
        {
            var c = Util.Compile($@"
                CONST INT VALUE_A = 1
                CONST INT VALUE_B = 2
                CONST INT VALUE_C = 3

                PROC MAIN()
                    INT n = 1

                    SWITCH n
                        CASE VALUE_A
                            n = 10
                        CASE VALUE_B
                            n = 20
                        CASE VALUE_C
                            n = 30
                        DEFAULT
                            n = 40
                    ENDSWITCH
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestRepeatedCase()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT n = 1

                    SWITCH n
                        CASE 1
                            n = 10
                        CASE 1
                            n = 20
                    ENDSWITCH
                ENDPROC
            ");

            Assert.True(c.GetAllDiagnostics().HasErrors, "Cannot have multiple CASEs with same value");
        }

        [Fact]
        public void TestRepeatedDefaultCase()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT n = 1

                    SWITCH n
                        CASE 1
                            n = 10
                        DEFAULT
                            n = 20
                        DEFAULT
                            n = 30
                    ENDSWITCH
                ENDPROC
            ");

            Assert.True(c.GetAllDiagnostics().HasErrors, "Cannot have multiple DEFAULT cases");
        }

        [Fact]
        public void TestNonIntTypes()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    FLOAT n = 1.0

                    SWITCH n
                        CASE 0.0
                            n = 10.0
                        CASE 1.0
                            n = 20.0
                        CASE 2.0
                            n = 30.0
                        DEFAULT
                            n = 40.0
                    ENDSWITCH
                ENDPROC
            ");

            Assert.True(c.GetAllDiagnostics().HasErrors, "Only INT type is supported");
        }
    }
}

namespace ScTools.Tests.ScriptLang
{
    using Xunit;

    public class ConstantTests
    {
        [Fact]
        public void TestConstant()
        {
            var c = Util.Compile($@"
                CONST INT SOME_VALUE = 10

                PROC MAIN()
                    INT a = SOME_VALUE
                    INT b[SOME_VALUE]
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestStatics()
        {
            var c = Util.Compile($@"
                CONST INT VALUE_A = 1
                CONST INT VALUE_B = 1 + VALUE_A
                CONST FLOAT VALUE_C = 0.5
                CONST BOOL VALUE_D = TRUE

                INT staticValue1 = VALUE_A + VALUE_B
                FLOAT staticValue2 = VALUE_C
                BOOL staticValue3 = VALUE_D

                PROC MAIN()
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
            Assert.Equal(1 + 2, c.CompiledScript.Statics[0].AsInt32);
            Assert.Equal(0.5f,  c.CompiledScript.Statics[1].AsFloat);
            Assert.Equal(1,     c.CompiledScript.Statics[2].AsInt32);
        }

        [Fact]
        public void TestStruct()
        {
            var c = Util.Compile($@"
                STRUCT SOME_DATA
                    INT A
                    FLOAT B
                    BOOL C
                ENDSTRUCT

                CONST SOME_DATA DATA = <<1, 0.5, TRUE>>

                INT staticValue1 = DATA.A
                FLOAT staticValue2 = DATA.B
                BOOL staticValue3 = DATA.C

                PROC MAIN()
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
            Assert.Equal(1,     c.CompiledScript.Statics[0].AsInt32);
            Assert.Equal(0.5f,  c.CompiledScript.Statics[1].AsFloat);
            Assert.Equal(1,     c.CompiledScript.Statics[2].AsInt32);
        }

        [Fact]
        public void TestCircularDefinition()
        {
            var module = Util.ParseAndAnalyze($@"
                CONST INT VALUE_A = 1 + VALUE_B
                CONST INT VALUE_B = 1 + VALUE_A

                PROC MAIN()
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors);
        }
    }
}

namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;

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
                CONST STRING VALUE_E = 'Hello World'

                INT staticValue1 = VALUE_A + VALUE_B
                FLOAT staticValue2 = VALUE_C
                BOOL staticValue3 = VALUE_D
                STRING staticValue4
                INT staticValue5[VALUE_B * VALUE_A]

                PROC MAIN()
                    staticValue4 = VALUE_E
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
            Assert.Equal(1 + 2, c.CompiledScript.Statics[0].AsInt32);
            Assert.Equal(0.5f,  c.CompiledScript.Statics[1].AsFloat);
            Assert.Equal(1,     c.CompiledScript.Statics[2].AsInt32);
            Assert.Equal(2 * 1, c.CompiledScript.Statics[4].AsInt32);
        }

        [Fact]
        public void TestImportedConstants()
        {
            const string Helper = @"
                CONST INT VALUE_A = 1
                CONST INT VALUE_B = 1 + VALUE_A
                CONST FLOAT VALUE_C = 0.5
                CONST BOOL VALUE_D = TRUE
                CONST STRING VALUE_E = 'Hello World'
            ";

            const string Script = @"
                USING 'helper.sch'

                INT staticValue1 = VALUE_A + VALUE_B
                FLOAT staticValue2 = VALUE_C
                BOOL staticValue3 = VALUE_D
                STRING staticValue4
                INT staticValue5[VALUE_B * VALUE_A]

                PROC MAIN()
                    staticValue4 = VALUE_E
                ENDPROC
            ";

            var resolver = new DelegatedUsingResolver(p => p switch {
                "helper.sch" => Helper,
                _ => null
            });

            var c = new Compilation { SourceResolver = resolver };
            c.SetMainModule(new StringReader(Script), filePath: "script.sc");
            c.Compile();

            Assert.False(c.GetAllDiagnostics().HasErrors);
            Assert.Equal(1 + 2, c.CompiledScript.Statics[0].AsInt32);
            Assert.Equal(0.5f,  c.CompiledScript.Statics[1].AsFloat);
            Assert.Equal(1,     c.CompiledScript.Statics[2].AsInt32);
            Assert.Equal(2 * 1, c.CompiledScript.Statics[4].AsInt32);
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

                PROC MAIN()
                ENDPROC
            ");

            Assert.True(c.GetAllDiagnostics().HasErrors, "Cannot have custom types as CONST");
        }

        [Fact(Skip = "Circular definitions in CONST throw an exception for now")]
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

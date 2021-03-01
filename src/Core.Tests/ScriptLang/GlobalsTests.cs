namespace ScTools.Tests.ScriptLang
{
    using ScTools.GameFiles;

    using Xunit;

    public class GlobalsTests
    {
        [Fact]
        public void TestBasic()
        {
            var c = Util.Compile($@"
                SCRIPT_NAME my_script
                SCRIPT_HASH 0x1234ABCD
                GLOBAL 1 my_script
                    INT g_iValue1 = 10, g_iValue2 = 20 - 5
                    FLOAT g_fValue3 = 3.3
                    VECTOR g_vValue4 = <<4.4, 5.5, 6.6>>
                ENDGLOBAL

                PROC MAIN()
                    INT v = g_iValue1 + g_iValue2
                    g_iValue1 = v
                ENDPROC
            ");

            var s = Util.Dump(c.CompiledScript);

            Assert.False(c.GetAllDiagnostics().HasErrors);
            Assert.Equal("my_script", c.CompiledScript.Name);
            Assert.Equal(0x1234ABCDu, c.CompiledScript.Hash);
            Assert.Equal(1u, c.CompiledScript.GlobalsBlock);
            Assert.Equal(6u, c.CompiledScript.GlobalsLength);
            Assert.Equal(10, c.CompiledScript.GlobalsPages[0][0].AsInt32);
            Assert.Equal(15, c.CompiledScript.GlobalsPages[0][1].AsInt32);
            Assert.Equal(3.3f, c.CompiledScript.GlobalsPages[0][2].AsFloat);
            Assert.Equal(4.4f, c.CompiledScript.GlobalsPages[0][3].AsFloat);
            Assert.Equal(5.5f, c.CompiledScript.GlobalsPages[0][4].AsFloat);
            Assert.Equal(6.6f, c.CompiledScript.GlobalsPages[0][5].AsFloat);
        }

        [Fact]
        public void TestMultiplePages()
        {
            var c = Util.Compile($@"
                SCRIPT_NAME my_script
                SCRIPT_HASH 0x1234ABCD
                GLOBAL 1 my_script
                    INT g_nOtherValue1 = 10
                    INT g_aValues1[0x3FFF]
                    INT g_nOtherValue2 = 20
                    INT g_aValues2[0x3FFF]
                    INT g_nOtherValue3 = 30
                ENDGLOBAL

                PROC MAIN()
                ENDPROC
            ");

            var s = Util.Dump(c.CompiledScript);

            Assert.False(c.GetAllDiagnostics().HasErrors);
            Assert.Equal("my_script", c.CompiledScript.Name);
            Assert.Equal(0x1234ABCDu, c.CompiledScript.Hash);
            Assert.Equal(1u,        c.CompiledScript.GlobalsBlock);
            Assert.Equal(1u + 0x4000 + 1 + 0x4000 + 1, c.CompiledScript.GlobalsLength);
            Assert.Equal(3,         c.CompiledScript.GlobalsPages.Count);
            Assert.Equal(Script.MaxPageLength, (uint)c.CompiledScript.GlobalsPages[0].Data.Length);
            Assert.Equal(Script.MaxPageLength, (uint)c.CompiledScript.GlobalsPages[1].Data.Length);
            Assert.Equal(3u,                   (uint)c.CompiledScript.GlobalsPages[2].Data.Length);
            Assert.Equal(10,        c.CompiledScript.GlobalsPages[0][0].AsInt32);
            Assert.Equal(0x3FFF,    c.CompiledScript.GlobalsPages[0][1].AsInt32);
            Assert.Equal(20,        c.CompiledScript.GlobalsPages[1][1].AsInt32);
            Assert.Equal(0x3FFF,    c.CompiledScript.GlobalsPages[1][2].AsInt32);
            Assert.Equal(30,        c.CompiledScript.GlobalsPages[2][2].AsInt32);
        }
    }
}

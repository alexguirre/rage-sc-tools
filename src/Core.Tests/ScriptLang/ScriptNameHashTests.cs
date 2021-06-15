namespace ScTools.Tests.ScriptLang
{
    using ScTools.ScriptLang;

    using Xunit;

    public class ScriptNameHashTests
    {
        //[Fact]
        //public void TestEmpty()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);

        //    Assert.Equal(Compilation.DefaultScriptName, c.CompiledScript.Name);
        //    Assert.Equal(0u, c.CompiledScript.Hash);
        //}

        //[Fact]
        //public void Test()
        //{
        //    var c = Util.Compile($@"
        //        SCRIPT_NAME my_script
        //        SCRIPT_HASH 0x1234ABCD

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);

        //    Assert.Equal("my_script", c.CompiledScript.Name);
        //    Assert.Equal(0x1234ABCDu, c.CompiledScript.Hash);
        //}

        //[Fact]
        //public void TestRepeatedName()
        //{
        //    var c = Util.Compile($@"
        //        SCRIPT_NAME my_script
        //        SCRIPT_NAME my_script2

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.True(c.GetAllDiagnostics().HasErrors, "Repeated SCRIPT_NAME");
        //}

        //[Fact]
        //public void TestRepeatedHash()
        //{
        //    var c = Util.Compile($@"
        //        SCRIPT_HASH 0x1234
        //        SCRIPT_HASH 0xABCD

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.True(c.GetAllDiagnostics().HasErrors, "Repeated SCRIPT_HASH");
        //}
    }
}

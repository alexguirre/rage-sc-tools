namespace ScTools.Tests.ScriptLang
{
    using ScTools.GameFiles;

    using Xunit;

    public class GlobalsTests
    {
        //[Fact]
        //public void TestBasic()
        //{
        //    var c = Util.Compile($@"
        //        SCRIPT_NAME my_script
        //        SCRIPT_HASH 0x1234ABCD
        //        GLOBAL 1 my_script
        //            INT g_iValue1 = 10, g_iValue2 = 20 - 5
        //            FLOAT g_fValue3 = 3.3
        //            VECTOR g_vValue4 = <<4.4, 5.5, 6.6>>
        //        ENDGLOBAL

        //        PROC MAIN()
        //            INT v = g_iValue1 + g_iValue2
        //            g_iValue1 = v
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //    Assert.Equal("my_script", c.CompiledScript.Name);
        //    Assert.Equal(0x1234ABCDu, c.CompiledScript.Hash);
        //    Assert.Equal(1u, c.CompiledScript.GlobalsBlock);
        //    Assert.Equal(6u, c.CompiledScript.GlobalsLength);
        //    Assert.Equal(10, c.CompiledScript.GlobalsPages[0][0].AsInt32);
        //    Assert.Equal(15, c.CompiledScript.GlobalsPages[0][1].AsInt32);
        //    Assert.Equal(3.3f, c.CompiledScript.GlobalsPages[0][2].AsFloat);
        //    Assert.Equal(4.4f, c.CompiledScript.GlobalsPages[0][3].AsFloat);
        //    Assert.Equal(5.5f, c.CompiledScript.GlobalsPages[0][4].AsFloat);
        //    Assert.Equal(6.6f, c.CompiledScript.GlobalsPages[0][5].AsFloat);
        //}

        //[Fact]
        //public void TestMultiplePages()
        //{
        //    var c = Util.Compile($@"
        //        SCRIPT_NAME my_script
        //        SCRIPT_HASH 0x1234ABCD
        //        GLOBAL 1 my_script
        //            INT g_nOtherValue1 = 10
        //            INT g_aValues1[0x3FFF]
        //            INT g_nOtherValue2 = 20
        //            INT g_aValues2[0x3FFF]
        //            INT g_nOtherValue3 = 30
        //        ENDGLOBAL

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //    Assert.Equal("my_script", c.CompiledScript.Name);
        //    Assert.Equal(0x1234ABCDu, c.CompiledScript.Hash);
        //    Assert.Equal(1u,        c.CompiledScript.GlobalsBlock);
        //    Assert.Equal(1u + 0x4000 + 1 + 0x4000 + 1, c.CompiledScript.GlobalsLength);
        //    Assert.Equal(3,         c.CompiledScript.GlobalsPages.Count);
        //    Assert.Equal(Script.MaxPageLength, (uint)c.CompiledScript.GlobalsPages[0].Data.Length);
        //    Assert.Equal(Script.MaxPageLength, (uint)c.CompiledScript.GlobalsPages[1].Data.Length);
        //    Assert.Equal(3u,                   (uint)c.CompiledScript.GlobalsPages[2].Data.Length);
        //    Assert.Equal(10,        c.CompiledScript.GlobalsPages[0][0].AsInt32);
        //    Assert.Equal(0x3FFF,    c.CompiledScript.GlobalsPages[0][1].AsInt32);
        //    Assert.Equal(20,        c.CompiledScript.GlobalsPages[1][1].AsInt32);
        //    Assert.Equal(0x3FFF,    c.CompiledScript.GlobalsPages[1][2].AsInt32);
        //    Assert.Equal(30,        c.CompiledScript.GlobalsPages[2][2].AsInt32);
        //}

        //[Fact]
        //public void TestFuncProtos()
        //{
        //    var c = Util.Compile($@"
        //        SCRIPT_NAME my_script
        //        SCRIPT_HASH 0x1234ABCD
        //        GLOBAL 1 my_script
        //            MY_FUNC_PROTO myFunc = MY_FUNC
        //        ENDGLOBAL

        //        PROTO FUNC INT MY_FUNC_PROTO()

        //        PROC MAIN()
        //        ENDPROC

        //        FUNC INT MY_FUNC()
        //            RETURN 10
        //        ENDFUNC
        //    ");

        //    Assert.True(c.GetAllDiagnostics().HasErrors, "Function prototypes are not allowed in global variables");
        //}

        //[Fact]
        //public void TestMultipleBlocks()
        //{
        //    const string Globals = @"
        //        GLOBAL 1 my_script
        //            INT g_nValue = 10
        //        ENDGLOBAL

        //        GLOBAL 2 other_script
        //            INT g_nOtherValue = 5
        //        ENDGLOBAL

        //        GLOBAL 3 another_script
        //            INT g_nAnotherValue = 5
        //        ENDGLOBAL
        //    ";

        //    const string Script = @"
        //        SCRIPT_NAME my_script
        //        SCRIPT_HASH 0x1234ABCD

        //        USING 'globals.sch'

        //        PROC MAIN()
        //            g_nOtherValue = g_nValue + g_nAnotherValue
        //        ENDPROC
        //    ";

        //    var c = Util.Compile(
        //        Script,
        //        sourceResolver: new DelegatedUsingResolver(p => p switch
        //        {
        //            "globals.sch" => Globals,
        //            _ => null
        //        }));

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //    Assert.Equal("my_script", c.CompiledScript.Name);
        //    Assert.Equal(0x1234ABCDu, c.CompiledScript.Hash);
        //    Assert.Equal(1u, c.CompiledScript.GlobalsBlock);
        //    Assert.Equal(1u, c.CompiledScript.GlobalsLength);
        //    Assert.Equal(10, c.CompiledScript.GlobalsPages[0][0].AsInt32);
        //}

        //[Fact]
        //public void TestMultipleBlocksRepeatedOwners()
        //{
        //    const string Globals1 = @"
        //        GLOBAL 1 my_script
        //            INT g_nValue = 10
        //        ENDGLOBAL
        //    ";
        //    const string Globals2 = @"
        //        GLOBAL 2 my_script
        //            INT g_nOtherValue = 5
        //        ENDGLOBAL
        //    ";
        //    const string Script = @"
        //        SCRIPT_NAME my_script
        //        SCRIPT_HASH 0x1234ABCD

        //        USING 'globals1.sch'
        //        USING 'globals2.sch'

        //        PROC MAIN()
        //            g_nOtherValue = g_nValue + g_nOtherValue
        //        ENDPROC
        //    ";

        //    var c = Util.Compile(
        //        Script,
        //        sourceResolver: new DelegatedUsingResolver(p => p switch
        //        {
        //            "globals1.sch" => Globals1,
        //            "globals2.sch" => Globals2,
        //            _ => null
        //        }));

        //    Assert.True(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestMultipleBlocksRepeatedIds()
        //{
        //    const string Globals1 = @"
        //        GLOBAL 1 my_script
        //            INT g_nValue = 10
        //        ENDGLOBAL
        //    ";
        //    const string Globals2 = @"
        //        GLOBAL 1 other_script
        //            INT g_nOtherValue = 5
        //        ENDGLOBAL
        //    ";
        //    const string Script = @"
        //        SCRIPT_NAME my_script
        //        SCRIPT_HASH 0x1234ABCD

        //        USING 'globals1.sch'
        //        USING 'globals2.sch'

        //        PROC MAIN()
        //            g_nOtherValue = g_nValue + g_nOtherValue
        //        ENDPROC
        //    ";

        //    var c = Util.Compile(
        //        Script,
        //        sourceResolver: new DelegatedUsingResolver(p => p switch
        //        {
        //            "globals1.sch" => Globals1,
        //            "globals2.sch" => Globals2,
        //            _ => null
        //        }));

        //    Assert.True(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestMaximumSize()
        //{
        //    var c = Util.Compile($@"
        //        SCRIPT_NAME my_script
        //        SCRIPT_HASH 0x1234ABCD
        //        GLOBAL 1 my_script
        //            INT g_aValues[{GlobalBlock.MaxSize - 1}]
        //        ENDGLOBAL

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //    Assert.Equal("my_script", c.CompiledScript.Name);
        //    Assert.Equal(0x1234ABCDu, c.CompiledScript.Hash);
        //    Assert.Equal(1u, c.CompiledScript.GlobalsBlock);
        //    Assert.Equal((uint)GlobalBlock.MaxSize, c.CompiledScript.GlobalsLength);
        //    Assert.Equal(GlobalBlock.MaxSize - 1, c.CompiledScript.GlobalsPages[0][0].AsInt32);
        //}

        //[Fact]
        //public void TestExceedMaximumSize()
        //{
        //    var c = Util.Compile($@"
        //        SCRIPT_NAME my_script
        //        SCRIPT_HASH 0x1234ABCD
        //        GLOBAL 1 my_script
        //            INT g_aValues[{GlobalBlock.MaxSize}]
        //        ENDGLOBAL

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.True(c.GetAllDiagnostics().HasErrors, "Global block exceeds max size");
        //}
    }
}

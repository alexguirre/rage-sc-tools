namespace ScTools.Tests.ScriptLang
{
    using Xunit;

    public class AnyTests
    {
        //[Fact]
        //public void TestLocal()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            ANY var
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestStatic()
        //{
        //    var c = Util.Compile($@"
        //        ANY v1 = 1
        //        ANY v2 = 2.0
        //        ANY v3 = TRUE

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //    Assert.NotNull(c.CompiledScript);
        //    Assert.Equal(1, c.CompiledScript.Statics[0].AsInt32);
        //    Assert.Equal(2.0, c.CompiledScript.Statics[1].AsFloat);
        //    Assert.Equal(1, c.CompiledScript.Statics[2].AsInt32);
        //}

        //[Fact]
        //public void TestAssignInt()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            ANY var = 1
        //            var = 2
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestAssignFloat()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            ANY var = 1.0
        //            var = 2.0
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestAssignBool()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            ANY var = TRUE
        //            var = FALSE
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestAssignString()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            ANY var = 'hello'
        //            var = 'world'
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestRef()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            INT n = 10
        //            FLOAT m = 5.0
        //            STRING s = 'hello world'
        //            VECTOR v = <<1.0, 2.0, 3.0>>

        //            ANY& ref1 = n
        //            ANY& ref2 = ref1
        //            ANY& ref3 = m
        //            ANY& ref4 = s
        //            ANY& ref5 = v
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestRefArgument()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            INT n = 10
        //            FLOAT m = 5.0
        //            STRING s = 'hello world'
        //            VECTOR v = <<1.0, 2.0, 3.0>>

        //            SOMETHING(n, m, s, v)
        //        ENDPROC

        //        PROC SOMETHING(ANY& ref1, ANY& ref2, ANY& ref3, ANY& ref4)
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestRefModify()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            INT n = 10
        //            ANY& ref = n
        //            ref = 5
        //        ENDPROC
        //    ");

        //    Assert.True(c.GetAllDiagnostics().HasErrors, "ANY references are read-only");
        //}
    }
}

namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;

    using Xunit;

    public class TypeCheckTests
    {
        //[Theory]
        //[InlineData("BOOL v = 4")]
        //[InlineData("INT v = TRUE")]
        //[InlineData("FLOAT v = 2")]
        //[InlineData("INT v = <<1.0, 2.0, 3.0>>")]
        //[InlineData("VECTOR v = 4.0")]
        //public void TestStaticInitializersIncorrectTypes(string staticDecl)
        //{
        //    var module = Util.ParseAndAnalyze($@"
        //        {staticDecl}

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.True(module.Diagnostics.HasErrors, $"Expected errors due to incorrect type in static initializer '{staticDecl}'");
        //}

        //[Theory]
        //[InlineData("BOOL v = TRUE")]
        //[InlineData("BOOL v = FALSE")]
        //[InlineData("BOOL v = 3 == 6")]
        //[InlineData("INT v = 3")]
        //[InlineData("FLOAT v = 1.0")]
        //[InlineData("FLOAT v = 2.0 + 3.0")]
        //[InlineData("VECTOR v = <<1.0, 2.0, 3.0>>")]
        //public void TestStaticInitializersCorrectTypes(string staticDecl)
        //{
        //    var module = Util.ParseAndAnalyze($@"
        //        {staticDecl}

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.False(module.Diagnostics.HasErrors, $"Expected no errors due to incorrect type in static initializer '{staticDecl}'");
        //}

        //[Theory]
        //[InlineData("BOOL v = 4")]
        //[InlineData("INT v = TRUE")]
        //[InlineData("FLOAT v = 2")]
        //[InlineData("INT v = <<1.0, 2.0, 3.0>>")]
        //[InlineData("VECTOR v = 4.0")]
        //public void TestGlobalInitializersIncorrectTypes(string decl)
        //{
        //    var module = Util.ParseAndAnalyze($@"
        //        GLOBAL 1 test
        //            {decl}
        //        ENDGLOBAL

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.True(module.Diagnostics.HasErrors, $"Expected errors due to incorrect type in global initializer '{decl}'");
        //}

        //[Theory]
        //[InlineData("BOOL v = TRUE")]
        //[InlineData("BOOL v = FALSE")]
        //[InlineData("BOOL v = 3 == 6")]
        //[InlineData("INT v = 3")]
        //[InlineData("FLOAT v = 1.0")]
        //[InlineData("FLOAT v = 2.0 + 3.0")]
        //[InlineData("VECTOR v = <<1.0, 2.0, 3.0>>")]
        //public void TestGlobalInitializersCorrectTypes(string decl)
        //{
        //    var module = Util.ParseAndAnalyze($@"
        //        GLOBAL 1 test
        //            {decl}
        //        ENDGLOBAL

        //        PROC MAIN()
        //        ENDPROC
        //    ");

        //    Assert.False(module.Diagnostics.HasErrors, $"Expected no errors due to incorrect type in global initializer '{decl}'");
        //}

        //[Theory]
        //[InlineData("INT v = DUMMY(5)")]
        //[InlineData("DUMMY(DUMMY(5))")]
        //[InlineData("INT v = 5 + DUMMY(5)")]
        //public void TestProcedureInExpression(string statement)
        //{
        //    var module = Util.ParseAndAnalyze($@"
        //        PROC MAIN()
        //            {statement}
        //        ENDPROC

        //        PROC DUMMY(INT v)
        //        ENDPROC
        //    ");

        //    Assert.True(module.Diagnostics.HasErrors, $"Expected error due to calling a procedure in an expression");
        //}

        //[Fact]
        //public void TestVectorExpression()
        //{
        //    var c1 = Util.Compile($@"
        //        PROC MAIN()
        //            VECTOR v = <<1.0, 2.0, 3.0>>
        //        ENDPROC
        //    ");
        //    Assert.False(c1.GetAllDiagnostics().HasErrors);

        //    var c2 = Util.Compile($@"
        //        PROC MAIN()
        //            FLOAT x = 1.0, y = 2.0, z = 3.0
        //            VECTOR v = <<x, y, z>>
        //        ENDPROC
        //    ");
        //    Assert.False(c2.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestVectorExpressionFromReferences()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            FLOAT x = 1.0, y = 2.0, z = 3.0
        //            FLOAt &xRef = x, &yRef = y, &zRef = z
        //            VECTOR v = <<xRef, yRef, zRef>>
        //        ENDPROC
        //    ");
        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestVectorExpressionIncorrectTypes()
        //{
        //    var c1 = Util.Compile($@"
        //        PROC MAIN()
        //            FLOAT y = 2.0, z = 3.0
        //            INT x = 1
        //            VECTOR v = <<x, y, z>>
        //        ENDPROC
        //    ");
        //    Assert.True(c1.GetAllDiagnostics().HasErrors);

        //    var c2 = Util.Compile($@"
        //        PROC MAIN()
        //            FLOAT x = 1.0, z = 3.0
        //            INT y = 2
        //            VECTOR v = <<x, y, z>>
        //        ENDPROC
        //    ");
        //    Assert.True(c2.GetAllDiagnostics().HasErrors);

        //    var c3 = Util.Compile($@"
        //        PROC MAIN()
        //            FLOAT x = 1.0, y = 2.0
        //            INT z = 3
        //            VECTOR v = <<x, y, z>>
        //        ENDPROC
        //    ");
        //    Assert.True(c3.GetAllDiagnostics().HasErrors);

        //    var c4 = Util.Compile($@"
        //        PROC MAIN()
        //            INT x = 1, y = 2, z = 3
        //            VECTOR v = <<x, y, z>>
        //        ENDPROC
        //    ");
        //    Assert.True(c4.GetAllDiagnostics().HasErrors);
        //}
    }
}

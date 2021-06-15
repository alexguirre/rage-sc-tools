namespace ScTools.Tests.ScriptLang
{
    using Xunit;

    public class LoopTests
    {
        //[Fact]
        //public void TestRepeat()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            INT n = 1

        //            INT i
        //            REPEAT n i
        //                DO_SOMETHING(i)
        //            ENDREPEAT

        //            REPEAT 5 i
        //                DO_SOMETHING(i)
        //            ENDREPEAT
        //        ENDPROC

        //        PROC DO_SOMETHING(INT x)
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestRepeatWithReferences()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            INT n = 1

        //            INT i

        //            INT &nRef = n
        //            INT &iRef = i
        //            REPEAT nRef iRef
        //                DO_SOMETHING(iRef)
        //            ENDREPEAT

        //            REPEAT 5 iRef
        //                DO_SOMETHING(iRef)
        //            ENDREPEAT
        //        ENDPROC

        //        PROC DO_SOMETHING(INT x)
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestWhile()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            INT n = 1

        //            INT i = 0
        //            WHILE i < 10
        //                DO_SOMETHING(i)
        //                i = i + 1
        //            ENDWHILE

        //            WHILE TRUE
        //                DO_SOMETHING(0)
        //            ENDWHILE
        //        ENDPROC

        //        PROC DO_SOMETHING(INT x)
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestWhileWithReferences()
        //{
        //    var c = Util.Compile($@"
        //        PROC MAIN()
        //            BOOL b = TRUE
        //            BOOL &bRef = b
        //            WHILE bRef
        //                DO_SOMETHING(0)
        //            ENDWHILE
        //        ENDPROC

        //        PROC DO_SOMETHING(INT x)
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}
    }
}

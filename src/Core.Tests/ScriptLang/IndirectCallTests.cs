namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;

    using Xunit;

    public class IndirectCallTests
    {
        //[Fact]
        //public void TestProc()
        //{
        //    var c = Util.Compile($@"
        //        PROTO PROC MY_PROCEDURE_PROTOTYPE(INT n, INT m)

        //        PROC MAIN()
        //            MY_PROCEDURE_PROTOTYPE myProc = MY_PROCEDURE
        //            myProc(1, 2)
        //        ENDPROC

        //        PROC MY_PROCEDURE(INT n, INT m)
        //            INT a = n + m
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestFunc()
        //{
        //    var c = Util.Compile($@"
        //        PROTO FUNC INT MY_FUNCTION_PROTOTYPE()

        //        PROC MAIN()
        //            MY_FUNCTION_PROTOTYPE myFunc = MY_FUNCTION
        //            myFunc()
        //            INT n = myFunc()
        //        ENDPROC

        //        FUNC INT MY_FUNCTION()
        //            RETURN 5
        //        ENDFUNC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestStatic()
        //{
        //    var c = Util.Compile($@"
        //        PROTO PROC MY_PROCEDURE_PROTOTYPE()

        //        MY_PROCEDURE_PROTOTYPE myProc

        //        PROC MAIN()
        //            myProc = MY_PROCEDURE
        //            myProc()
        //        ENDPROC

        //        PROC MY_PROCEDURE()
        //            INT a = 5
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestStaticInitializer()
        //{
        //    var c = Util.Compile($@"
        //        PROTO PROC MY_PROCEDURE_PROTOTYPE()

        //        MY_PROCEDURE_PROTOTYPE myProc = MY_PROCEDURE

        //        PROC MAIN()
        //            myProc()
        //        ENDPROC

        //        PROC MY_PROCEDURE()
        //            INT a = 5
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestStruct()
        //{
        //    var c = Util.Compile($@"
        //        PROTO PROC MY_PROCEDURE_PROTOTYPE(INT n, INT m)

        //        STRUCT MY_STRUCTURE
        //            INT arg1
        //            MY_PROCEDURE_PROTOTYPE cb
        //        ENDSTRUCT

        //        PROC MAIN()
        //            MY_STRUCTURE myStruct
        //            myStruct.arg1 = 1
        //            myStruct.cb = MY_PROCEDURE

        //            myStruct.cb(myStruct.arg1, 2)
        //        ENDPROC

        //        PROC MY_PROCEDURE(INT n, INT m)
        //            INT a = n + m
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}

        //[Fact]
        //public void TestArray()
        //{
        //    var c = Util.Compile($@"
        //        PROTO PROC MY_PROCEDURE_PROTOTYPE(INT n, INT m)

        //        PROC MAIN()
        //            MY_PROCEDURE_PROTOTYPE myProcs[2]
        //            myProcs[0] = MY_PROCEDURE_1
        //            myProcs[1] = MY_PROCEDURE_2
        //            myProcs[0](1, 2)
        //            myProcs[1](3, 4)
        //        ENDPROC

        //        PROC MY_PROCEDURE_1(INT n, INT m)
        //            INT a = n + m
        //        ENDPROC

        //        PROC MY_PROCEDURE_2(INT n, INT m)
        //            INT a = n * m
        //        ENDPROC
        //    ");

        //    Assert.False(c.GetAllDiagnostics().HasErrors);
        //}
    }
}

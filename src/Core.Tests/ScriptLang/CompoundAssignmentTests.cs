namespace ScTools.Tests.ScriptLang
{
    using System.Linq;

    using Xunit;

    public class CompoundAssignmentTests
    {
        //[Fact]
        //public void TestConversionToEntityIndex()
        //{
        //    var c1 = Util.Compile($@"
        //        PROC MAIN()
        //            INT n = 5
        //            INT m = 10
        //            n += m
        //            n -= m
        //            n *= m
        //            n /= m
        //            n %= m
        //            n |= m
        //            n &= m
        //            n ^= m
        //        ENDPROC
        //    ");
        //    Assert.False(c1.GetAllDiagnostics().HasErrors);

        //    var c2 = Util.Compile($@"
        //        PROC MAIN()
        //            INT n = 5
        //            INT m = 10
        //            n = n + m
        //            n = n - m
        //            n = n * m
        //            n = n / m
        //            n = n % m
        //            n = n | m
        //            n = n & m
        //            n = n ^ m
        //        ENDPROC
        //    ");
        //    Assert.False(c2.GetAllDiagnostics().HasErrors);

        //    // same bytecode
        //    Assert.Equal(c2.CompiledScript.CodeLength, c1.CompiledScript.CodeLength);
        //    foreach (var (p1, p2) in c1.CompiledScript.CodePages.Items.Zip(c2.CompiledScript.CodePages.Items)
        //                                                              .Select(p => (p.First.Data, p.Second.Data)))
        //    {
        //        Assert.Equal(p2, p1);
        //    }
        //}
    }
}

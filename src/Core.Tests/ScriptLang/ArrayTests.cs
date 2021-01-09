namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;

    using Xunit;

    public class ArrayTests
    {
        [Fact]
        public void TestArray()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    INT a[4]
                    a[0] = 1
                    a[1] = a[0] + 1
                    a[2] = a[1] + 1
                    a[3] = a[2] + 1
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestStaticArray()
        {
            var c = Util.Compile($@"
                STRUCT MY_STRUCT
                    FLOAT data[4]
                ENDSTRUCT

                INT a[4]
                MY_STRUCT b[2]

                PROC MAIN()
                    a[0] = 1
                    a[1] = a[0] + 1
                    a[2] = a[1] + 1
                    a[3] = a[2] + 1

                    b[0].data[0] = 1.0
                    b[0].data[1] = 2.0
                    b[0].data[2] = 3.0
                    b[0].data[3] = 4.0
                    b[1] = b[0]
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
            Assert.NotNull(c.CompiledScript);
            Assert.Equal(4, c.CompiledScript.Statics[0].AsInt32);
            Assert.Equal(2, c.CompiledScript.Statics[5].AsInt32);
            Assert.Equal(4, c.CompiledScript.Statics[5 + 1].AsInt32);
            Assert.Equal(4, c.CompiledScript.Statics[5 + 6].AsInt32);
        }

        [Fact(Skip = "Array initializers not supported yet")]
        public void TestArrayInitializer()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    INT a[4] = <<1, 2, 3, 4>>
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact(Skip = "Array initializers not supported yet")]
        public void TestStaticArrayInitializer()
        {
            var c = Util.Compile($@"
                INT a[4] = <<1, 2, 3, 4>>

                PROC MAIN()
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
            Assert.NotNull(c.CompiledScript);
            Assert.Equal(4, c.CompiledScript.Statics[0].AsInt32);
            Assert.Equal(1, c.CompiledScript.Statics[1].AsInt32);
            Assert.Equal(2, c.CompiledScript.Statics[2].AsInt32);
            Assert.Equal(3, c.CompiledScript.Statics[3].AsInt32);
            Assert.Equal(4, c.CompiledScript.Statics[4].AsInt32);
        }

        [Fact]
        public void TestArrayAssignment()
        {
            var module = Util.ParseAndAnalyze($@"
                INT a[4]

                PROC MAIN()
                    INT b[4]

                    b[0] = 1
                    b[1] = 2
                    b[2] = 3
                    b[3] = 4
                    a = b
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void Test2dArray()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    INT a[3][2]
                    a[0][0] = 1
                    a[0][1] = a[0][0] + 1
                    a[1][0] = a[0][1] + 1
                    a[1][1] = a[1][0] + 1
                    a[2][0] = a[1][1] + 1
                    a[2][1] = a[2][0] + 1
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestArrayOfStructs()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    VECTOR a[3]
                    a[0] = <<1.0, 2.0, 3.0>>
                    a[1] = <<4.0, 5.0, 6.0>>
                    a[2] = <<7.0, 8.0, 9.0>>
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefToArrayItem()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT a[3]
                    a[0] = 1
                    a[1] = 2
                    a[2] = 3

                    INT& b = a[1]
                    b = 4 + b
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestArrayOfRefs()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    INT& arr[3]
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors, "No arrays of references");
        }

        [Fact]
        public void TestArrayLengthField()
        {
            var c = Util.Compile($@"
                PROC MAIN()
                    INT a[3]
                    INT b[2][3]

                    INT i = 0
                    WHILE i < a.length
                        a[i] = a.length + i + b.length + b[0].length

                        i = i + 1
                    ENDWHILE
                ENDPROC
            ");

            Assert.False(c.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestArrayFieldDifferentToLength()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    INT a[3]

                    INT i = a.someField
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors, "Arrays only have 'length' field");
        }

        [Fact]
        public void TestNegativeLength()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    INT a[-4]
                    INT b[5 - 10]
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors, "Arrays cannot have negative length");
        }
    }
}

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
            var module = Util.ParseAndAnalyze($@"
                INT a[4]

                PROC MAIN()
                    a[0] = 1
                    a[1] = a[0] + 1
                    a[2] = a[1] + 1
                    a[3] = a[2] + 1
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestArrayInitializer()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    INT a[4] = <<1, 2, 3, 4>>
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestStaticArrayInitializer()
        {
            var module = Util.ParseAndAnalyze($@"
                INT a[4] = <<1, 2, 3, 4>>

                PROC MAIN()
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestArrayAssignment()
        {
            var module = Util.ParseAndAnalyze($@"
                INT a[4]

                PROC MAIN()
                    INT b[4]

                    a = <<1, 2, 3, 4>>
                    b = <<5, 6, 7, 8>>
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

                    a = <<<<1, 2>>, <<3, 4>>, <<5, 6>>>>
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
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
    }
}

namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;

    using Xunit;

    public class RefTypeTests
    {
        [Fact]
        public void TestRefInStaticVariable()
        {
            var module = Util.ParseAndAnalyze($@"
                FLOAT& v

                PROC MAIN()
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors, $"Ref types are not supported as static variables");
        }

        [Fact]
        public void TestRefInStruct()
        {
            var module = Util.ParseAndAnalyze($@"
                STRUCT MY_STRUCT
                    FLOAT& v
                ENDSTRUCT

                FLOAT value = 0.5

                PROC MAIN()
                    MY_STRUCT s = <<value>>
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefInStructInStaticVariable()
        {
            var module = Util.ParseAndAnalyze($@"
                STRUCT MY_STRUCT
                    FLOAT& v
                ENDSTRUCT

                MY_STRUCT s

                PROC MAIN()
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors, $"Ref types are not supported as static variables");
        }

        [Fact]
        public void TestRefMissingInitializer()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    FLOAT& a
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors, $"Ref type variable needs to be initialized always");
        }

        [Fact]
        public void TestRefInStructMissingInitializer()
        {
            var module = Util.ParseAndAnalyze($@"
                STRUCT MY_STRUCT
                    FLOAT& v
                ENDSTRUCT

                PROC MAIN()
                    MY_STRUCT s
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors, $"Refs need to be initialized always");
        }

        [Fact]
        public void TestRefInLocalVariable()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    FLOAT a = 5.0
                    FLOAT& b = a
                    b = 2.0
                    FLOAT c = a + b
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefIncorrectInitializerExpr()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    FLOAT& a = 4.0 + 5.0
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefAssignFromRef()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    FLOAT a = 1.0
                    FLOAT& b = a
                    FLOAT& c = b
                    c = 2.0
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefInParameter()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    FLOAT a = 5.0
                    DO_SOMETHING(a)

                    FLOAT& b = a
                    DO_SOMETHING(b)
                ENDPROC

                PROC DO_SOMETHING(FLOAT& b)
                    b = 2.0
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefInNative()
        {
            var module = Util.ParseAndAnalyze($@"
                NATIVE PROC DELETE_PED(INT& handle)

                PROC MAIN()
                    INT a = 1
                    DELETE_PED(a)
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefToStruct()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    VEC3 a = <<1.0, 2.0, 3.0>>
                    DO_SOMETHING(a)

                    VEC3& b = a
                    DO_SOMETHING(b)
                    b.y = 5.0
                    b.z = 6.0
                ENDPROC

                PROC DO_SOMETHING(VEC3& b)
                    b.x = 4.0
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefToArray()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    INT a[4]
                    a[0] = 1
                    a[1] = 2
                    a[2] = 3
                    a[3] = 4
                    DO_SOMETHING(a)

                    INT (&b)[4] = a
                    DO_SOMETHING(b)
                    b[3] = 10
                ENDPROC

                PROC DO_SOMETHING(INT (&arr)[4])
                    arr[0] = arr[0] + 1
                    arr[1] = arr[1] + 1
                    arr[2] = arr[2] + 1
                    arr[3] = arr[3] + 1
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestAssignToRefStruct()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    VEC3 a = <<1.0, 2.0, 3.0>>
                    VEC3& b = a
                    VEC3 c = <<4.0, 5.0, 6.0>>
                    b = c
                    b = <<7.0, 8.0, 9.0>>
                ENDPROC
            ");

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefToRef()
        {
            var module = Util.ParseAndAnalyze($@"
                PROC MAIN()
                    FLOAT a = 1.0
                    FLOAT &b = a
                    FLOAT &(&c) = b
                    FLOAT &&d = b
                ENDPROC
            ");

            Assert.True(module.Diagnostics.HasErrors, "No references to references");
        }
    }
}

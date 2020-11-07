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
            var module = Module.Compile(new StringReader($@"
                FLOAT& v

                PROC MAIN()
                ENDPROC
            "));

            Assert.True(module.Diagnostics.HasErrors, $"Ref types are not supported as static variables");
        }

        [Fact]
        public void TestRefInStruct()
        {
            var module = Module.Compile(new StringReader($@"
                STRUCT MY_STRUCT
                    FLOAT& v
                ENDSTRUCT

                PROC MAIN()
                ENDPROC
            "));

            Assert.True(module.Diagnostics.HasErrors, $"Ref type are not supported as struct fields");
        }

        [Fact]
        public void TestRefMissingInitializer()
        {
            var module = Module.Compile(new StringReader($@"
                PROC MAIN()
                    FLOAT& a
                ENDPROC
            "));

            Assert.True(module.Diagnostics.HasErrors, $"Ref type variable needs to be initialized always");
        }

        [Fact]
        public void TestRefInLocalVariable()
        {
            var module = Module.Compile(new StringReader($@"
                PROC MAIN()
                    FLOAT a = 5.0
                    FLOAT& b = a
                    b = 2.0
                    FLOAT c = a + b
                ENDPROC
            "));

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefIncorrectInitializerExpr()
        {
            var module = Module.Compile(new StringReader($@"
                PROC MAIN()
                    FLOAT& a = 4.0 + 5.0
                ENDPROC
            "));

            Assert.True(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefAssignFromRef()
        {
            var module = Module.Compile(new StringReader($@"
                PROC MAIN()
                    FLOAT a = 1.0
                    FLOAT& b = a
                    FLOAT& c = b
                    c = 2.0
                ENDPROC
            "));

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefInParameter()
        {
            var module = Module.Compile(new StringReader($@"
                PROC MAIN()
                    FLOAT a = 5.0
                    DO_SOMETHING(a)

                    FLOAT& b = a
                    DO_SOMETHING(b)
                ENDPROC

                PROC DO_SOMETHING(FLOAT& b)
                    b = 2.0
                ENDPROC
            "));

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefInNative()
        {
            var module = Module.Parse(new StringReader($@"
                NATIVE PROC DELETE_PED(INT& handle)

                PROC MAIN()
                    INT a = 1
                    DELETE_PED(a)
                ENDPROC
            "));

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestRefToStruct()
        {
            var module = Module.Compile(new StringReader($@"
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
            "));

            Assert.False(module.Diagnostics.HasErrors);
        }

        [Fact]
        public void TestAssignToRefStruct()
        {
            var module = Module.Compile(new StringReader($@"
                PROC MAIN()
                    VEC3 a = <<1.0, 2.0, 3.0>>
                    VEC3& b = a
                    VEC3 c = <<4.0, 5.0, 6.0>>
                    b = c
                    b = <<7.0, 8.0, 9.0>>
                ENDPROC
            "));

            Assert.False(module.Diagnostics.HasErrors);
        }
    }
}

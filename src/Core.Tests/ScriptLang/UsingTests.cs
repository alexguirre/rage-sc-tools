namespace ScTools.Tests.ScriptLang
{
    using System;
    using System.IO;
    using System.Collections.Generic;

    using ScTools.ScriptLang;

    using Xunit;

    public class UsingTests
    {
        [Fact]
        public void TestSimple()
        {
            const string Helper = @"
                INT someValue = 42

                FUNC INT ADD_NUMBERS(INT n, INT m)
                    RETURN n + m
                ENDFUNC
            ";

            const string Script = @"
                USING 'helper.sch'

                PROC MAIN()
                    INT n = ADD_NUMBERS(someValue, 8)
                ENDPROC
            ";

            var resolver = new DelegatedUsingResolver(p => p switch {
                "helper.sch" => Helper,
                _ => null
            });

            var comp = new Compilation { SourceResolver = resolver };
            comp.SetMainModule(new StringReader(Script), filePath: "script.sc");
            comp.Compile();

            Assert.False(comp.GetAllDiagnostics().HasErrors);
        }

        [Fact]
        public void TestNonexistant()
        {
            const string Script = @"
                USING 'unknown.sch'

                PROC MAIN()
                ENDPROC
            ";

            var resolver = new DelegatedUsingResolver(p => null);

            var comp = new Compilation { SourceResolver = resolver };
            comp.SetMainModule(new StringReader(Script), filePath: "script.sc");
            comp.Compile();

            Assert.True(comp.GetAllDiagnostics().HasErrors);
        }

        [Fact(Skip = "Circular USINGs not supported yet")]
        public void TestCircular()
        {
            // TODO: support circular USINGs?
            const string Helper1 = @"
                USING 'helper2.sch'

                INT someValue = 42

                FUNC INT MULT_SUB(INT n)
                    RETURN someValue - MULT(n)
                ENDFUNC
            ";

            const string Helper2 = @"
                USING 'helper1.sch'

                FUNC INT ADD_NUMBERS(INT n, INT m)
                    RETURN n + m
                ENDFUNC

                FUNC INT MULT(INT n)
                    RETURN someValue * n
                ENDFUNC
            ";

            const string Script = @"
                USING 'helper2.sch'

                PROC MAIN()
                    INT n = ADD_NUMBERS(someValue, 8)
                    someValue = MULT(2)
                    someValue = MULT_SUB(4)
                ENDPROC
            ";

            var resolver = new DelegatedUsingResolver(p => p switch { 
                "helper1.sch" => Helper1,
                "helper2.sch" => Helper2,
                _ => null
            });

            var comp = new Compilation { SourceResolver = resolver };
            comp.SetMainModule(new StringReader(Script), filePath: "script.sc");
            comp.Compile();

            Assert.False(comp.GetAllDiagnostics().HasErrors);
        }

        private sealed class DelegatedUsingResolver : IUsingSourceResolver
        {
            public Func<string, string> Resolver { get; }

            public DelegatedUsingResolver(Func<string, string> resolver) => Resolver = resolver;

            public string NormalizePath(string usingPath) => usingPath;
            public bool IsValid(string usingPath) => Resolver(usingPath) != null;
            public bool HasChanged(string usingPath) => false;
            public TextReader Resolve(string usingPath) => new StringReader(Resolver(usingPath));
        }
    }
}

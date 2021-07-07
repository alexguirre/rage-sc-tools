namespace ScTools.Tests.ScriptLang
{
    using System.IO;
    using System.Linq;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Semantics;

    using Xunit;

    public class IdentificationTests
    {
        [Fact]
        public void TestMissingScript()
        {
            var d = ParseAndIdentify($@"");

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        [Fact]
        public void TestKnownNative()
        {
            var nativeDB = NativeDB.FromJson(@"
            {
                ""TranslationTable"": [[1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317]],
                ""HashToRows"": [{ ""Hash"": 1234605617868164317, ""Rows"": [ 0 ] }],
                ""Commands"": [{
                      ""Hash"": 1234605617868164317,
                      ""Name"": ""TEST"",
                      ""Build"": 323,
                      ""Parameters"": [{ ""Type"": ""int"", ""Name"": ""a""}, { ""Type"": ""int"", ""Name"": ""b""}],
                      ""ReturnType"": ""void""
                    }
                ]
            }");

            var d = ParseAndIdentify($@"
                NATIVE PROC TEST(INT a, INT b)

                SCRIPT test_script
                ENDSCRIPT
            ", nativeDB);

            Assert.False(d.HasErrors);
        }

        [Fact]
        public void TestUnknownNative()
        {
            var nativeDB = NativeDB.FromJson(@"
            {
                ""TranslationTable"": [[1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317, 1234605617868164317]],
                ""HashToRows"": [{ ""Hash"": 1234605617868164317, ""Rows"": [ 0 ] }],
                ""Commands"": [{
                      ""Hash"": 1234605617868164317,
                      ""Name"": ""TEST"",
                      ""Build"": 323,
                      ""Parameters"": [{ ""Type"": ""int"", ""Name"": ""a""}, { ""Type"": ""int"", ""Name"": ""b""}],
                      ""ReturnType"": ""void""
                    }
                ]
            }");

            var d = ParseAndIdentify($@"
                NATIVE PROC UNKNOWN_TEST(INT a, INT b)

                SCRIPT test_script
                ENDSCRIPT
            ", nativeDB);

            Assert.True(d.HasErrors);
            Assert.Single(d.Errors);
        }

        private static DiagnosticsReport ParseAndIdentify(string source, NativeDB? nativeDB = null)
        {
            using var sourceReader = new StringReader($@"
                {source}");
            var d = new DiagnosticsReport();
            var p = new Parser(sourceReader, "test.sc");
            p.Parse(d);

            var globalSymbols = GlobalSymbolTableBuilder.Build(p.OutputAst, d);
            IdentificationVisitor.Visit(p.OutputAst, d, globalSymbols, nativeDB ?? NativeDB.Empty);
            return d;
        }
    }
}

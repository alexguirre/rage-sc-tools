namespace ScTools.Tests
{
    using System;
    using System.IO;
    using System.Linq;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang;

    using Xunit;

    internal static class Util
    {
        public static Module ParseAndAnalyze(string src, string path = "test.sc")
        {
            var m = new Module(path);
            using var r = new StringReader(src);
            m.Parse(r);
            m.DoFirstSemanticAnalysisPass(null);
            m.DoSecondSemanticAnalysisPass();
            m.DoBinding();
            return m;
        }

        public static Compilation Compile(string src, string path = "test.sc", IUsingSourceResolver? sourceResolver = null)
        {
            var c = new Compilation { SourceResolver = sourceResolver };
            using var r = new StringReader(src);
            c.SetMainModule(r, path);
            c.Compile();
            return c;
        }

        public static Assembler Assemble(string src, string path = "test.sc")
        {
            using var r = new StringReader(src);
            return Assembler.Assemble(r, path);
        }

        public static string Disassemble(GameFiles.Script sc)
        {
            using var w = new StringWriter();
            Disassembler.Disassemble(w, sc);
            return w.ToString();
        }

        public static string Dump(GameFiles.Script sc)
        {
            var d = new Dumper(sc);
            using var s = new StringWriter();
            d.Dump(s, true, true, true, true, true);
            return s.ToString();
        }

        public static void AssertScriptsAreEqual(GameFiles.Script sc1, GameFiles.Script sc2)
        {
            Assert.Equal(sc1.Hash, sc2.Hash);
            Assert.Equal(sc1.Name, sc2.Name);
            Assert.Equal(sc1.NameHash, sc2.NameHash);
            Assert.Equal(sc1.NumRefs, sc2.NumRefs);

            Assert.Equal(sc1.CodeLength, sc2.CodeLength);
            foreach (var (page1, page2) in sc1.CodePages.Zip(sc2.CodePages))
            {
                Assert.Equal(page1.Data, page2.Data);
            }

            Assert.Equal(sc1.StaticsCount, sc2.StaticsCount);
            Assert.Equal(sc1.ArgsCount, sc2.ArgsCount);
            foreach (var (static1, static2) in sc1.Statics.Zip(sc2.Statics))
            {
                Assert.Equal(static1.AsUInt64, static2.AsUInt64);
            }

            Assert.Equal(sc1.GlobalsLength, sc2.GlobalsLength);
            Assert.Equal(sc1.GlobalsBlock, sc2.GlobalsBlock);
            foreach (var (page1, page2) in sc1.GlobalsPages.Zip(sc2.GlobalsPages))
            {
                foreach (var (global1, global2) in page1.Data.Zip(page2.Data))
                {
                    Assert.Equal(global1.AsUInt64, global2.AsUInt64);
                }
            }

            Assert.Equal(sc1.NativesCount, sc2.NativesCount);
            Assert.Equal(sc1.Natives, sc2.Natives);

            Assert.Equal(sc1.StringsLength, sc2.StringsLength);
            foreach (var (page1, page2) in sc1.StringsPages.Zip(sc2.StringsPages))
            {
                Assert.Equal(page1.Data, page2.Data);
            }
        }
    }

    internal sealed class DelegatedUsingResolver : IUsingSourceResolver
    {
        public Func<string, string> Resolver { get; }

        public DelegatedUsingResolver(Func<string, string> resolver) => Resolver = resolver;

        public string NormalizePath(string usingPath) => usingPath;
        public bool IsValid(string usingPath) => Resolver(usingPath) != null;
        public bool HasChanged(string usingPath) => false;
        public TextReader Resolve(string usingPath) => new StringReader(Resolver(usingPath));
    }
}

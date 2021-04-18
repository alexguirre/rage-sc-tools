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
            static void FailIf(bool condition, string message) => Assert.False(condition, message);
            static string S(string str) => str;

            FailIf(sc1.Hash != sc2.Hash, S("Hash is different"));
            FailIf(sc1.Name != sc2.Name, S("Name is different"));
            FailIf(sc1.NameHash != sc2.NameHash, S("NameHash is different"));
            FailIf(sc1.NumRefs != sc2.NumRefs, S("NumRefs is different"));

            FailIf(sc1.CodeLength != sc2.CodeLength, S("CodeLength is different"));
            if (sc1.CodePages != null && sc2.CodePages != null)
            {
                var codePageIdx = 0;
                foreach (var (page1, page2) in sc1.CodePages.Zip(sc2.CodePages))
                {
                    var equal = Enumerable.SequenceEqual(page1.Data, page2.Data);
                    FailIf(!equal, S($"CodePage #{codePageIdx} is different"));
                    if (!equal)
                    {
                        //File.WriteAllText($"test_original_code_page_{codePageIdx}.txt", string.Join(' ', page1.Data.Select(b => b.ToString("X2"))));
                        //File.WriteAllText($"test_new_code_page_{codePageIdx}.txt", string.Join(' ', page2.Data.Select(b => b.ToString("X2"))));
                    }
                    codePageIdx++;
                }
            }

            FailIf(sc1.StaticsCount != sc2.StaticsCount, S("StaticsCount is different"));
            FailIf(sc1.ArgsCount != sc2.ArgsCount, S("ArgsCount is different"));
            FailIf((sc1.Statics != null) != (sc2.Statics != null), S("Statics null"));
            if (sc1.Statics != null && sc2.Statics != null)
            {
                var staticIdx = 0;
                foreach (var (static1, static2) in sc1.Statics.Zip(sc2.Statics))
                {
                    FailIf(static1.AsUInt64 != static2.AsUInt64, S($"Static #{staticIdx} is different"));
                    staticIdx++;
                }
            }

            FailIf(sc1.GlobalsLength != sc2.GlobalsLength, S("GlobalsLength is different"));
            FailIf(sc1.GlobalsBlock != sc2.GlobalsBlock, S("GlobalsBlock is different"));
            FailIf((sc1.GlobalsPages != null) != (sc2.GlobalsPages != null), S("Globals null"));
            if (sc1.GlobalsPages != null && sc2.GlobalsPages != null)
            {
                var globalIdx = 0;
                foreach (var (page1, page2) in sc1.GlobalsPages.Zip(sc2.GlobalsPages))
                {
                    foreach (var (global1, global2) in page1.Data.Zip(page2.Data))
                    {
                        FailIf(global1.AsUInt64 != global2.AsUInt64, S($"Global #{globalIdx} is different"));
                        globalIdx++;
                    }
                }
            }

            FailIf(sc1.NativesCount != sc2.NativesCount, S("NativesCount is different"));
            FailIf((sc1.Natives != null) != (sc2.Natives != null), S("Natives null"));
            if (sc1.Natives != null && sc2.Natives != null)
            {
                FailIf(!Enumerable.SequenceEqual(sc1.Natives, sc2.Natives), S("Natives is different"));
            }

            FailIf(sc1.StringsLength != sc2.StringsLength, S("StringsLength is different"));
            if (sc1.StringsPages != null && sc2.StringsPages != null)
            {
                var stringPageIdx = 0;
                foreach (var (page1, page2) in sc1.StringsPages.Zip(sc2.StringsPages))
                {
                    FailIf(!Enumerable.SequenceEqual(page1.Data, page2.Data), S($"StringsPage #{stringPageIdx} is different"));
                    stringPageIdx++;
                }
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

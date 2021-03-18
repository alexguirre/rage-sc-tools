namespace ScTools.Tests
{
    using System;
    using System.IO;

    using ScTools.ScriptLang;

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

        public static GameFiles.Script Assemble(string src)
        {
            return new GameFiles.Script();
        }

        public static string Dump(GameFiles.Script sc)
        {
            var d = new Dumper(sc);
            using var s = new StringWriter();
            d.Dump(s, true, true, true, true, true);
            return s.ToString();
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

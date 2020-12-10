namespace ScTools.Tests
{
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
    }
}

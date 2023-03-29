namespace ScTools.Playground
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using ScTools;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang;
    using ScTools.ScriptLang.CodeGen;
    //using ScTools.ScriptLang.Semantics;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            //LoadGTA5Keys();
            DoTest();
        }

        public static void DoTest()
        {
            //NativeDB.Fetch(new Uri("https://raw.githubusercontent.com/alloc8or/gta5-nativedb-data/master/natives.json"), "ScriptHookV_1.0.2060.1.zip")
            //    .ContinueWith(t => File.WriteAllText("nativedb.json", t.Result.ToJson()))
            //    .Wait();

            //var nativeDB = NativeDB.FromJson(System.IO.File.ReadAllText("nativesdb.json"));

            const string BaseDir = "D:\\sources\\gtav-sc-tools\\examples\\language_sample\\";

            Parse(BaseDir + "language_sample_main.sc");
            //Parse(BaseDir + "language_sample_child.sc", nativeDB);
            //Parse(BaseDir + "language_sample_shared.sch");

            ;

        }

        private static void Parse(string filePath)
        {
            using var r = new StreamReader(filePath);
            var d = new DiagnosticsReport();
            var lexer = new ScriptLang.Lexer(filePath, r.ReadToEnd(), d);
            var tokens = lexer.ToArray();
            foreach (var token in tokens)
            {
                //Console.WriteLine($"{token.Kind}\t=\t`{token.Lexeme.Span.Escape()}`");
            }
            Console.WriteLine();
            //d.PrintAll(Console.Out);

            ;
        }
    }
}

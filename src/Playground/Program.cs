namespace ScTools.Playground
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Antlr4.Runtime;

    using CodeWalker.GameFiles;

    using ScTools;
    using ScTools.GameFiles;
    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Grammar;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Semantics.Symbols;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            //LoadGTA5Keys();
            DoTest();
        }

        private static void LoadGTA5Keys()
        {
            string path = ".\\Keys";
            GTA5Keys.PC_AES_KEY = File.ReadAllBytes(path + "\\gtav_aes_key.dat");
            GTA5Keys.PC_NG_KEYS = CryptoIO.ReadNgKeys(path + "\\gtav_ng_key.dat");
            GTA5Keys.PC_NG_DECRYPT_TABLES = CryptoIO.ReadNgTables(path + "\\gtav_ng_decrypt_tables.dat");
            GTA5Keys.PC_NG_ENCRYPT_TABLES = CryptoIO.ReadNgTables(path + "\\gtav_ng_encrypt_tables.dat");
            GTA5Keys.PC_NG_ENCRYPT_LUTs = CryptoIO.ReadNgLuts(path + "\\gtav_ng_encrypt_luts.dat");
            GTA5Keys.PC_LUT = File.ReadAllBytes(path + "\\gtav_hash_lut.dat");
        }

        public static void DoTest()
        {
            //NativeDB.Fetch(new Uri("https://raw.githubusercontent.com/alloc8or/gta5-nativedb-data/master/natives.json"), "ScriptHookV_1.0.2060.1.zip")
            //    .ContinueWith(t => File.WriteAllText("nativedb.json", t.Result.ToJson()))
            //    .Wait();

            //var nativeDB = NativeDB.FromJson(System.IO.File.ReadAllText("nativesdb.json"));

            const string BaseDir = "D:\\sources\\gtav-sc-tools\\examples\\language_sample\\";

            Parse(BaseDir + "language_sample_main.sc");
            Parse(BaseDir + "language_sample_child.sc");
            Parse(BaseDir + "language_sample_shared.sch");

            ;

        }

        private static void Parse(string filePath)
        {
            using var reader = new StreamReader(filePath);
            AntlrInputStream inputStream = new AntlrInputStream(reader);

            DiagnosticsReport diagnostics = new(filePath);
            ScLangLexer lexer = new(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new SyntaxErrorListener<int>(diagnostics));
            CommonTokenStream tokens = new(lexer);
            ScLangParser parser = new(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new SyntaxErrorListener<IToken>(diagnostics));

            PrintParseTree(Console.Out, parser, parser.program());
            parser.program();

            diagnostics.PrintAll(Console.Out);
        }

        private static void PrintParseTree(TextWriter w, Parser p, ParserRuleContext ctx)
        {
            w.WriteLine(ctx.ToInfoString(p));
        }

        private sealed class SyntaxErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
        {
            private readonly DiagnosticsReport diagnostics;

            public SyntaxErrorListener(DiagnosticsReport diagnostics) => this.diagnostics = diagnostics;

            public void SyntaxError(TextWriter output, IRecognizer recognizer, TSymbol offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                var source = offendingSymbol is IToken t ?
                                SourceRange.FromTokens(t, null) :
                                new SourceRange(new SourceLocation(line, charPositionInLine),
                                                new SourceLocation(line, charPositionInLine));
                diagnostics.AddError(msg, source);
            }
        }
    }
}

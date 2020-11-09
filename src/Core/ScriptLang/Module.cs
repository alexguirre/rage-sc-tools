#nullable enable
namespace ScTools.ScriptLang
{
    using System.Diagnostics;
    using System.IO;

    using Antlr4.Runtime;

    using ScTools.GameFiles;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Grammar;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Semantics.Binding;
    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class Module
    {
        public Root Ast { get; }
        public SymbolTable SymbolTable { get; }
        public BoundModule BoundModule { get; }
        public DiagnosticsReport Diagnostics { get; }
        /// <summary>
        /// Gets the file path included in diagnostics messages.
        /// </summary>
        public string FilePath { get; }
        public Script CompiledScript { get; }

        private Module(TextReader input, string filePath, NativeDB? nativeDB, bool compile)
        {
            Diagnostics = new DiagnosticsReport();
            FilePath = filePath;

            Ast = Parse(input);
            DoSyntaxCheck();
            (SymbolTable, BoundModule) = DoSemanticAnalysis();
            CompiledScript = (!compile || Diagnostics.HasErrors) ? CreateEmptyScript() :
                                                                   Compile(nativeDB);
        }

        public string GetAstDotGraph() => AstDotGenerator.Generate(Ast);

        private Root Parse(TextReader input)
        {
            AntlrInputStream inputStream = new AntlrInputStream(input);

            ScLangLexer lexer = new ScLangLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new SyntaxErrorListener<int>(this));
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            ScLangParser parser = new ScLangParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new SyntaxErrorListener<IToken>(this));
            return (Root)parser.script().Accept(new AstBuilder());
        }

        private void DoSyntaxCheck()
        {
            Diagnostics.AddFrom(SyntaxChecker.Check(Ast, FilePath));
        }

        private (SymbolTable, BoundModule) DoSemanticAnalysis()
        {
            var (diagnostics, symbols, boundModule) = SemanticAnalysis.Visit(Ast, FilePath);
            Diagnostics.AddFrom(diagnostics);
            return (symbols, boundModule);
        }

        private Script Compile(NativeDB? nativeDB)
        {
            Debug.Assert(!Diagnostics.HasErrors);
            return BoundModule.Assemble(nativeDB);
        }

        private static Script CreateEmptyScript()
            => new Script
            {
                Hash = 0, // TODO: how is this hash calculated?
                ArgsCount = 0,
                StaticsCount = 0,
                GlobalsLengthAndBlock = 0,
                NativesCount = 0,
                Name = EmptyScriptName,
                NameHash = EmptyScriptName.ToHash(),
                StringsLength = 0,
            };

        private const string EmptyScriptName = "unknown";

        public static Module Compile(TextReader input, string filePath = "tmp.sc", NativeDB? nativeDB = null)
            => new Module(input, filePath, nativeDB, compile: true);

        public static Module Parse(TextReader input, string filePath = "tmp.sc", NativeDB? nativeDB = null)
            => new Module(input, filePath, nativeDB, compile: false);


        /// <summary>
        /// Adds syntax errors to diagnostics report.
        /// </summary>
        private sealed class SyntaxErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
        {
            private readonly Module mod;

            public SyntaxErrorListener(Module mod) => this.mod = mod;

            public void SyntaxError(TextWriter output, IRecognizer recognizer, TSymbol offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                var source = offendingSymbol is IToken t ?
                                SourceRange.FromTokens(t, null) :
                                new SourceRange(new SourceLocation(line, charPositionInLine),
                                                new SourceLocation(line, charPositionInLine));
                mod.Diagnostics.AddError(mod.FilePath, msg, source);
            }
        }
    }
}

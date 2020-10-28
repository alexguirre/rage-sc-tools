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
    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class Module
    {
        public Root Ast { get; }
        public SymbolTable SymbolTable { get; }
        public int StaticVarsTotalSize { get; }
        public DiagnosticsReport Diagnostics { get; }
        /// <summary>
        /// Gets the file path included in diagnostics messages.
        /// </summary>
        public string FilePath { get; }
        public Script CompiledScript { get; }

        private Module(TextReader input, string filePath)
        {
            Diagnostics = new DiagnosticsReport();
            FilePath = filePath;

            Ast = Parse(input);
            if (Diagnostics.HasErrors)
            {
                // create empty defaults in case of parsing errors
                SymbolTable = new SymbolTable();
                StaticVarsTotalSize = -1;
                CompiledScript = CreateEmptyScript();
            }
            else
            {
                DoSyntaxCheck();
                (SymbolTable, StaticVarsTotalSize) = DoSemanticAnalysis();
                CompiledScript = Diagnostics.HasErrors ? CreateEmptyScript() :
                                                         Compile();
            }
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

        private (SymbolTable, int StaticVarsTotalSize) DoSemanticAnalysis()
        {
            var (diagnostics, symbols, staticsTotalSize) = SemanticAnalysis.Visit(Ast, FilePath);
            Diagnostics.AddFrom(diagnostics);
            return (symbols, staticsTotalSize);
        }

        private Script Compile()
        {
            Debug.Assert(!Diagnostics.HasErrors);
            var sc = CreateEmptyScript();
            sc.StaticsCount = (uint)StaticVarsTotalSize;
            sc.Statics = new ScriptValue[StaticVarsTotalSize];

            var code = new ByteCodeBuilder();
            foreach (var topStmt in Ast.Statements)
            {
                switch (topStmt)
                {
                    case ScriptNameStatement s:
                        sc.Name = s.Name;
                        sc.NameHash = s.Name.ToHash();
                        break;
                    case FunctionStatement _:
                    case ProcedureStatement _:
                        var name = (topStmt as FunctionStatement)?.Name ??
                                   (topStmt as ProcedureStatement)!.Name;
                        Debug.Assert(name != null);

                        var func = SymbolTable.Lookup(name) as FunctionSymbol;
                        Debug.Assert(func != null);

                        code.BeginFunction(func.Name);

                        code.EndFunction();
                        break;
                }
            }

            // TODO
            return sc;
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

        public static Module Compile(TextReader input, string filePath = "tmp.sc")
            => new Module(input, filePath);


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

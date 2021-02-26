#nullable enable
namespace ScTools.ScriptLang
{
    using System;
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

    public enum ModuleState
    {
        None = 0,
        Parsed,
        SemanticAnalysisFirstPassDone,
        SemanticAnalysisSecondPassDone,
        Bound,
    }

    public sealed class Module
    {
        public ModuleState State { get; private set; } = ModuleState.None;
        public Root? Ast { get; private set; }
        public SymbolTable? SymbolTable { get; private set; }
        public BoundModule? BoundModule { get; private set; }
        public DiagnosticsReport Diagnostics { get; }
        /// <summary>
        /// Gets the file path included in diagnostics messages.
        /// </summary>
        public string FilePath { get; }

        public Module(string filePath)
        {
            Diagnostics = new DiagnosticsReport();
            FilePath = filePath;
        }

        public string GetAstDotGraph() => Ast != null ? AstDotGenerator.Generate(Ast) : string.Empty;

        public void Parse(TextReader input)
        {
            if (State != ModuleState.None)
            {
                throw new InvalidOperationException("Invalid state");
            }

            AntlrInputStream inputStream = new AntlrInputStream(input);

            ScLangLexer lexer = new ScLangLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new SyntaxErrorListener<int>(this));
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            ScLangParser parser = new ScLangParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new SyntaxErrorListener<IToken>(this));
            Ast = AstBuilder.Build(parser.script());
            SyntaxChecker.Check(Ast, FilePath, Diagnostics);
            State = ModuleState.Parsed;
        }

        public void DoFirstSemanticAnalysisPass(IUsingModuleResolver? usingResolver)
        {
            if (State != ModuleState.Parsed)
            {
                throw new InvalidOperationException("Invalid state");
            }

            Debug.Assert(Ast != null);
            SymbolTable = new SymbolTable(Ast);

            SemanticAnalysis.DoFirstPass(Ast, FilePath, SymbolTable, usingResolver, Diagnostics);
            State = ModuleState.SemanticAnalysisFirstPassDone;
        }

        public void DoSecondSemanticAnalysisPass()
        {
            if (State != ModuleState.SemanticAnalysisFirstPassDone)
            {
                throw new InvalidOperationException("Invalid state");
            }

            Debug.Assert(Ast != null);
            Debug.Assert(SymbolTable != null);

            SemanticAnalysis.DoSecondPass(Ast, FilePath, SymbolTable, Diagnostics);
            State = ModuleState.SemanticAnalysisSecondPassDone;
        }

        public void DoBinding()
        {
            if (State != ModuleState.SemanticAnalysisSecondPassDone)
            {
                throw new InvalidOperationException("Invalid state");
            }

            Debug.Assert(Ast != null);
            Debug.Assert(SymbolTable != null);

            BoundModule = SemanticAnalysis.DoBinding(Ast, FilePath, SymbolTable, Diagnostics);
            State = ModuleState.Bound;
        }

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

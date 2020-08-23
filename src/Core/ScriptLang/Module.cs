#nullable enable
namespace ScTools.ScriptLang
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Antlr4.Runtime;

    using ScTools.GameFiles;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Grammar;
    using ScTools.ScriptLang.Semantics;

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
            DoSyntaxCheck();
            (SymbolTable, StaticVarsTotalSize) = DoSemanticAnalysis();
            CompiledScript = Diagnostics.HasErrors ? CreateEmptyScript() : Compile();
        }

        public string GetAstDotGraph() => AstDotGenerator.Generate(Ast);

        private Root Parse(TextReader input)
        {
            AntlrInputStream inputStream = new AntlrInputStream(input);

            ScLangLexer lexer = new ScLangLexer(inputStream);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            ScLangParser parser = new ScLangParser(tokens);
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
    }
}

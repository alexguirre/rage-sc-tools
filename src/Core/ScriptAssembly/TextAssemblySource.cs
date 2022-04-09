#nullable enable
namespace ScTools.ScriptAssembly
{
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;

    using ScTools.ScriptAssembly.Grammar;

    public class TextAssemblySource : IAssemblySource
    {
        public string FilePath { get; }
        public TextReader Input { get; }

        public TextAssemblySource(TextReader input, string fileName)
        {
            FilePath = fileName;
            Input = input;
        }

        public void Produce(DiagnosticsReport diagnostics, IAssemblySource.ConsumeLineDelegate consumeLine)
            => Parse(diagnostics, consumeLine);

        private void Parse(DiagnosticsReport diagnostics, IAssemblySource.ConsumeLineDelegate consumeLine)
        {
            var inputStream = new LightInputStream(Input.ReadToEnd());

            var lexer = new ScAsmLexer(inputStream) { TokenFactory = new LightTokenFactory() };
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new SyntaxErrorListener<int>(FilePath, diagnostics));
            var tokens = new CommonTokenStream(lexer);
            var parser = new ScAsmParser(tokens) { BuildParseTree = false };
            parser.AddParseListener(new ParseListener(this, parser, consumeLine));
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new SyntaxErrorListener<IToken>(FilePath, diagnostics));

            parser.program();
        }

        private sealed class ParseListener : ScAsmBaseListener
        {
            private readonly TextAssemblySource src;
            private readonly ScAsmParser parser;
            private readonly IAssemblySource.ConsumeLineDelegate consumeLine;
            private ScAsmParser.ProgramContext? programContext;

            public ParseListener(TextAssemblySource src, ScAsmParser parser, IAssemblySource.ConsumeLineDelegate consumeLine)
                => (this.src, this.parser, this.consumeLine) = (src, parser, consumeLine);

            public override void EnterProgram([NotNull] ScAsmParser.ProgramContext context)
            {
                programContext = context;
            }

            public override void EnterLine([NotNull] ScAsmParser.LineContext context)
            {
                parser.BuildParseTree = true;
            }

            public override void ExitLine([NotNull] ScAsmParser.LineContext context)
            {
                Debug.Assert(programContext != null);

                consumeLine(src, context);
                if (programContext.ChildCount > 0)
                {
                    programContext.RemoveLastChild();
                }
                ClearChildren(context);
                parser.BuildParseTree = false;
            }

            public override void ExitProgram([NotNull] ScAsmParser.ProgramContext context)
            {
                programContext = null;
                context.children = null;
            }

            private static void ClearChildren(ParserRuleContext context)
            {
                if (context.children != null)
                {
                    foreach (var child in context.children.OfType<ParserRuleContext>())
                    {
                        ClearChildren(child);
                    }
                    context.children = null;
                }
            }
        }

        /// <summary>
        /// Adds syntax errors to a diagnostics report.
        /// </summary>
        private sealed class SyntaxErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
        {
            private readonly string filePath;
            private readonly DiagnosticsReport diagnostics;

            public SyntaxErrorListener(string filePath, DiagnosticsReport diagnostics)
                => (this.filePath, this.diagnostics) = (filePath, diagnostics);

            public void SyntaxError(TextWriter output, IRecognizer recognizer, TSymbol offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                var source = offendingSymbol is IToken t ?
                                SourceRange.FromTokens(filePath, t, null) :
                                new SourceRange(new SourceLocation(line, charPositionInLine, filePath),
                                                new SourceLocation(line, charPositionInLine, filePath));
                diagnostics.AddError(msg, source);
            }
        }
    }
}

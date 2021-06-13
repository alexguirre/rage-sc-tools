using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Antlr4.Runtime;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Directives;
using ScTools.ScriptLang.Grammar;

namespace ScTools.ScriptLang
{
    public class Parser
    {
        public string FilePath { get; }
        public TextReader Input { get; }
        public Program OutputAst { get; private set; }

        public Parser(TextReader input, string fileName)
        {
            FilePath = fileName;
            Input = input;
            OutputAst = new Program(SourceRange.Unknown, Enumerable.Empty<IDirective>());
        }

        public void Parse(Diagnostics diagnostics)
        {
            var inputStream = new AntlrInputStream(Input);

            var diagnosticsReport = diagnostics.GetReport(FilePath);
            var lexer = new ScLangLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new SyntaxErrorListener<int>(diagnosticsReport));
            var tokens = new CommonTokenStream(lexer);
            var parser = new ScLangParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new SyntaxErrorListener<IToken>(diagnosticsReport));

            OutputAst = BuildAst(parser.program());
        }

        private Program BuildAst(ScLangParser.ProgramContext context)
            => new(Source(context), context.directive().Select(BuildAst));

        private IDirective BuildAst(ScLangParser.DirectiveContext context)
            => context switch
            {
                ScLangParser.ScriptHashDirectiveContext c => new ScriptHashDirective(Source(c), Parse(c.integer())),
                ScLangParser.ScriptNameDirectiveContext c => new ScriptNameDirective(Source(c), c.identifier().GetText()),
                ScLangParser.UsingDirectiveContext c => new UsingDirective(Source(c), Parse(c.@string())),
                _ => throw new NotSupportedException(),
            };

        private static SourceRange Source(ParserRuleContext context) => SourceRange.FromTokens(context.Start, context.Stop);

        private static string Parse(ScLangParser.StringContext context)
        {
            var s = context.GetText();
            return s.AsSpan(1, s.Length - 2).Unescape();
        }

        private static int Parse(ScLangParser.IntegerContext context)
        {
            var s = context.GetText();
            if (s.Length > 2 && s[0] == '`' && s[^1] == '`')
            {
                // if starts and ends with '`', it is a hashed string
                return unchecked((int)s.AsSpan(1, s.Length - 2).Unescape().ToLowercaseHash());
            }
            else
            {
                // dec or hex int
                return s.ParseAsInt();
            }
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

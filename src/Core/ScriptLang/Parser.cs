namespace ScTools.ScriptLang
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Antlr4.Runtime;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Grammar;

    public class Parser
    {
        public string FilePath { get; }
        public TextReader Input { get; }
        public Program OutputAst { get; private set; }
        private bool scriptNameSet = false, scriptHashSet = false;

        public Parser(TextReader input, string fileName)
        {
            FilePath = fileName;
            Input = input;
            OutputAst = new Program(SourceRange.Unknown);
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

            ProcessParseTree(parser.program(), diagnosticsReport);
        }

        private void ProcessParseTree(ScLangParser.ProgramContext context, DiagnosticsReport diagnostics)
        {
            foreach (var directive in context.directive())
            {
                ProcessDirective(directive, diagnostics);
            }

            foreach (var astDecl in context.declaration().SelectMany(BuildAst))
            {
                OutputAst.Declarations.Add(astDecl);
            }
        }

        private void ProcessDirective(ScLangParser.DirectiveContext context, DiagnosticsReport diagnostics)
        {
            switch (context)
            {
                case ScLangParser.ScriptHashDirectiveContext h:
                    if (!scriptHashSet)
                    {
                        OutputAst.ScriptHash = Parse(h.integer());
                        scriptHashSet = true;
                    }
                    else
                    {
                        diagnostics.AddError("SCRIPT_HASH directive is repeated", Source(h));
                    }
                    break;
                case ScLangParser.ScriptNameDirectiveContext n:
                    if (!scriptNameSet)
                    {
                        OutputAst.ScriptName = n.identifier().GetText();
                        scriptNameSet = true;
                    }
                    else
                    {
                        diagnostics.AddError("SCRIPT_NAME directive is repeated", Source(n));
                    }
                    break;
                case ScLangParser.UsingDirectiveContext u:
                    var path = Parse(u.@string());
                    // TODO: handle USING directives
                    break;
                default: throw new NotSupportedException();
            }
        }

        private IEnumerable<IDeclaration> BuildAst(ScLangParser.DeclarationContext context)
        {
            switch (context)
            {
                case ScLangParser.EnumDeclarationContext c:
                    var enumMembers = c.enumList().enumMemberDeclarationList()
                                       .SelectMany(l => l.enumMemberDeclaration())
                                       .Select(m => new EnumMemberDeclaration(Source(m), m.identifier().GetText(), BuildAst(m.initializer)));
                    yield return new EnumDeclaration(Source(c), c.identifier().GetText(), enumMembers);
                    break;
                default: break; // TODO: throw new NotSupportedException();
            }
        }

        private IExpression? BuildAst(ScLangParser.ExpressionContext context)
        {
            // TODO
            return null;
        }

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

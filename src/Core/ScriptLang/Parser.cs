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
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.Grammar;

    public interface IUsingResolver
    {
        /// <summary>
        /// Returns the source code of <paramref name="usingPath"/>.
        /// </summary>
        /// <param name="originFilePath">The path of the file that contains the USING with <paramref name="usingPath"/>.</param>
        (Func<TextReader> Open, string FilePath) Resolve(string originFilePath, string usingPath);
    }

    public class Parser
    {
        public string FilePath { get; }
        public TextReader Input { get; }
        public IUsingResolver? UsingResolver { get; set; }
        public Program OutputAst { get; private set; }
        private HashSet<string> usings = new(); // TODO: handle including the initial file from another file
        private bool scriptNameSet = false, scriptHashSet = false;

        public Parser(TextReader input, string fileName)
        {
            FilePath = fileName;
            Input = input;
            OutputAst = new Program(SourceRange.Unknown);
        }

        public void Parse(Diagnostics diagnostics)
        {
            ParseFile(Input, FilePath, diagnostics);
        }

        private void ParseFile(TextReader input, string filePath, Diagnostics diagnostics)
        {
            var inputStream = new AntlrInputStream(input);

            var diagnosticsReport = diagnostics[filePath];
            var lexer = new ScLangLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new SyntaxErrorListener<int>(diagnosticsReport));
            var tokens = new CommonTokenStream(lexer);
            var parser = new ScLangParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new SyntaxErrorListener<IToken>(diagnosticsReport));

            ProcessParseTree(parser.program(), filePath, diagnostics);
        }

        private void ProcessParseTree(ScLangParser.ProgramContext context, string filePath, Diagnostics diagnostics)
        {
            foreach (var directive in context.directive())
            {
                ProcessDirective(directive, filePath, diagnostics);
            }

            foreach (var astDecl in context.declaration().SelectMany(BuildAst))
            {
                OutputAst.Declarations.Add(astDecl);
            }
        }

        private void ProcessDirective(ScLangParser.DirectiveContext context, string filePath, Diagnostics diagnostics)
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
                        diagnostics[filePath].AddError("SCRIPT_HASH directive is repeated", Source(h));
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
                        diagnostics[filePath].AddError("SCRIPT_NAME directive is repeated", Source(n));
                    }
                    break;
                case ScLangParser.UsingDirectiveContext u:
                    if (UsingResolver == null)
                    {
                        diagnostics[filePath].AddWarning($"USING directive but {nameof(Parser)}.{nameof(UsingResolver)} is not set", Source(u));
                    }
                    else
                    {
                        var usingPath = Parse(u.@string());
                        var (open, newFilePath) = UsingResolver.Resolve(filePath, usingPath);
                        if (usings.Add(newFilePath))
                        {
                            using var newInput = open();
                            ParseFile(newInput, newFilePath, diagnostics);
                        }
                    }
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
                case ScLangParser.ConstantVariableDeclarationContext c:
                    foreach (var varDecl in BuildVarDecls(VarKind.Constant, c.varDeclaration()))
                    {
                        yield return varDecl;
                    }
                    break;
                case ScLangParser.ArgVariableDeclarationContext c:
                    foreach (var varDecl in BuildVarDecls(VarKind.StaticArg, c.varDeclaration()))
                    {
                        yield return varDecl;
                    }
                    break;
                case ScLangParser.StaticVariableDeclarationContext c:
                    foreach (var varDecl in BuildVarDecls(VarKind.Static, c.varDeclaration()))
                    {
                        yield return varDecl;
                    }
                    break;
                default: break; // TODO: throw new NotSupportedException();
            }
        }

        private IExpression? BuildAst(ScLangParser.ExpressionContext? context)
        {
            // TODO
            return null;
        }

        private IEnumerable<VarDeclaration> BuildVarDecls(VarKind kind, ScLangParser.VarDeclarationContext varDeclContext)
        {
            return varDeclContext.initDeclaratorList().initDeclarator()
                .Select(initDecl => new VarDeclaration(Source(initDecl), GetNameFromDeclarator(initDecl.declarator()), kind)
                {
                    Type = BuildTypeFromDeclarator(varDeclContext.type.GetText(), initDecl.declarator()),
                    Initializer = BuildAst(initDecl.initializer),
                });
        }

        private IType BuildTypeFromDeclarator(string baseType, ScLangParser.DeclaratorContext declarator)
        {
            // TODO
            return IType.Unknown;
        }

        public string GetNameFromDeclarator(ScLangParser.DeclaratorContext declarator)
        {
            return (declarator.refDeclarator(), declarator.noRefDeclarator()) switch
            {
                (var refDecl, null) => GetRefName(refDecl),
                (null, var noRefDecl) => GetNoRefName(noRefDecl),
                _ => throw new NotSupportedException(),
            };

            static string GetRefName(ScLangParser.RefDeclaratorContext refDeclarator)
                => GetNoRefName(refDeclarator.noRefDeclarator());

            static string GetNoRefName(ScLangParser.NoRefDeclaratorContext noRefDeclarator)
                => noRefDeclarator switch
                {
                    ScLangParser.SimpleDeclaratorContext s => s.identifier().GetText(),
                    ScLangParser.ArrayDeclaratorContext a => GetNoRefName(a.noRefDeclarator()),
                    ScLangParser.ParenthesizedRefDeclaratorContext p => GetRefName(p.refDeclarator()),
                    _ => throw new NotSupportedException(),
                };
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

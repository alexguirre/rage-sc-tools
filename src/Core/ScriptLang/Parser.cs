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
    using ScTools.ScriptLang.Ast.Statements;
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
                            // merge the AST from the included file
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
                case ScLangParser.ProcedureDeclarationContext c:
                    var pTy = BuildFuncType(Source(c), null, c.parameterList());
                    yield return new FuncDeclaration(Source(c), c.name.GetText(), FuncKind.UserDefined)
                    {
                        Type = pTy,
                        Body = BuildFuncBody(pTy, c.statementBlock()),
                    };
                    break;
                case ScLangParser.FunctionDeclarationContext c:
                    var fTy = BuildFuncType(Source(c), c.returnType, c.parameterList());
                    yield return new FuncDeclaration(Source(c), c.name.GetText(), FuncKind.UserDefined)
                    {
                        Type = fTy,
                        Body = BuildFuncBody(fTy, c.statementBlock()),
                    };
                    break;
                case ScLangParser.ProcedureNativeDeclarationContext c:
                    yield return new FuncDeclaration(Source(c), c.name.GetText(), FuncKind.Native)
                    {
                        Type = BuildFuncType(Source(c), null, c.parameterList()),
                    };
                    break;
                case ScLangParser.FunctionNativeDeclarationContext c:
                    yield return new FuncDeclaration(Source(c), c.name.GetText(), FuncKind.Native)
                    {
                        Type = BuildFuncType(Source(c), c.returnType, c.parameterList()),
                    };
                    break;
                case ScLangParser.ProcedurePrototypeDeclarationContext c:
                    yield return new FuncProtoDeclaration(Source(c), c.name.GetText())
                    {
                        DeclaredType = BuildFuncType(Source(c), null, c.parameterList()),
                    };
                    break;
                case ScLangParser.FunctionPrototypeDeclarationContext c:
                    yield return new FuncProtoDeclaration(Source(c), c.name.GetText())
                    {
                        DeclaredType = BuildFuncType(Source(c), c.returnType, c.parameterList()),
                    };
                    break;
                case ScLangParser.EnumDeclarationContext c:
                    var enumMembers = c.enumList().enumMemberDeclarationList()
                                       .SelectMany(l => l.enumMemberDeclaration())
                                       .Select(m => new EnumMemberDeclaration(Source(m), m.identifier().GetText(), BuildAstOpt(m.initializer)));
                    yield return new EnumDeclaration(Source(c), c.identifier().GetText(), enumMembers);
                    break;
                case ScLangParser.ConstantVariableDeclarationContext c:
                    foreach (var varDecl in BuildVarDecls(VarKind.Constant, c.varDeclaration()))
                    {
                        // TODO: check if initializer expression in CONST var is missing
                        yield return varDecl;
                    }
                    break;
                case ScLangParser.ArgVariableDeclarationContext c:
                    // TODO: rethink ARG variables grammar to allow static initialization of multiple values
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

        private IExpression? BuildAstOpt(ScLangParser.ExpressionContext? context)
            => context is not null ? BuildAst(context) : null;

        private IExpression BuildAst(ScLangParser.ExpressionContext context)
        {
            // TODO: BuildAst(ScLangParser.ExpressionContext)
            return null;
        }

        private IEnumerable<IStatement> BuildAst(ScLangParser.LabeledStatementContext context)
        {
            if (context.label() is not null and var label)
            {
                yield return new LabelDeclaration(Source(label), label.identifier().GetText());
            }

            // TODO: BuildAst(ScLangParser.LabeledStatementContext)
            switch (context.statement())
            {
                case null: break;
                case ScLangParser.VariableDeclarationStatementContext c:
                    foreach (var varDecl in BuildVarDecls(VarKind.Local, c.varDeclaration()))
                    {
                        yield return varDecl;
                    }
                    break;
                case ScLangParser.IfStatementContext c:
                    var ifStmt = new IfStatement(Source(c), BuildAst(c.condition))
                    {
                        Then = new List<IStatement>(c.thenBlock.labeledStatement().SelectMany(BuildAst))
                    };

                    // convert ELIFs to nested IFs inside ELSE
                    var currIfStmt = ifStmt;
                    foreach (var elifBlock in c.elifBlock())
                    {
                        var innerIfStmt = new IfStatement(Source(elifBlock), BuildAst(elifBlock.condition))
                        {
                            Then = new List<IStatement>(elifBlock.statementBlock().labeledStatement().SelectMany(BuildAst))
                        };
                        currIfStmt.Else = new List<IStatement> { innerIfStmt };
                        currIfStmt = innerIfStmt;
                    }

                    if (c.elseBlock() is not null and var elseBlock)
                    {
                        currIfStmt.Else = new List<IStatement>(elseBlock.statementBlock().labeledStatement().SelectMany(BuildAst));
                    }

                    yield return ifStmt;
                    break;
                case ScLangParser.RepeatStatementContext c:
                    yield return new RepeatStatement(Source(c), BuildAst(c.limit), BuildAst(c.counter))
                    {
                        Body = new List<IStatement>(c.statementBlock().labeledStatement().SelectMany(BuildAst))
                    };
                    break;
                case ScLangParser.WhileStatementContext c:
                    yield return new WhileStatement(Source(c), BuildAst(c.condition))
                    {
                        Body = new List<IStatement>(c.statementBlock().labeledStatement().SelectMany(BuildAst))
                    };
                    break;
                default: break; // TODO: throw new NotSupportedException();
            }
        }

        private IEnumerable<VarDeclaration> BuildVarDecls(VarKind kind, ScLangParser.VarDeclarationContext varDeclContext)
        {
            return varDeclContext.initDeclaratorList().initDeclarator()
                .Select(initDecl => new VarDeclaration(Source(initDecl), GetNameFromDeclarator(initDecl.declarator()), kind)
                {
                    Type = BuildTypeFromDeclarator(varDeclContext.type, initDecl.declarator()),
                    Initializer = BuildAstOpt(initDecl.initializer),
                });
        }

        private List<IStatement> BuildFuncBody(FuncType funcType, ScLangParser.StatementBlockContext statementBlockContext)
        {
            // include parameter var declarations at the start of the function body
            var paramsDecls = funcType.Parameters
                                      .Select(p => new VarDeclaration(p.Source, p.Name, VarKind.Parameter)
                                        {
                                            Type = p.Type,
                                            Initializer = null,
                                        });
            var statements = statementBlockContext.labeledStatement().SelectMany(BuildAst);
            return new List<IStatement>(paramsDecls.Concat(statements));
        }

        private FuncType BuildFuncType(SourceRange source, ScLangParser.IdentifierContext? returnType, ScLangParser.ParameterListContext paramsContext)
        {
            return new FuncType(source)
            {
                ReturnType = returnType is null ? null : BuildType(returnType),
                Parameters = new List<FuncTypeParameter>(
                    paramsContext.singleVarDeclarationNoInit()
                                 .Select(pDecl => new FuncTypeParameter(Source(pDecl),
                                                                        GetNameFromDeclarator(pDecl.declarator()),
                                                                        BuildTypeFromDeclarator(pDecl.type, pDecl.declarator())))),
            };
        }

        private IType BuildTypeFromDeclarator(ScLangParser.IdentifierContext baseTypeName, ScLangParser.DeclaratorContext declarator)
        {
            // TODO: BuildTypeFromDeclarator
            return IType.Unknown;
        }

        private IType BuildType(ScLangParser.IdentifierContext typeName)
        {
            // TODO: BuildType
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

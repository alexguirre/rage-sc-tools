namespace ScTools.ScriptLang
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Antlr4.Runtime;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.Grammar;

    public interface IUsingResolver
    {
        string NormalizeFilePath(string filePath);

        /// <summary>
        /// Returns the source code of <paramref name="usingPath"/>.
        /// </summary>
        /// <param name="originFilePath">The path of the file that contains the USING with <paramref name="usingPath"/>.</param>
        /// <param name="usingPath">The path specified in the USING directive.</param>
        /// <returns>
        /// Tuple with a <see cref="Func{TResult}"/> that returns a <see cref="TextReader"/> with the source code of the
        /// resolved file and the normalized path of the file.
        /// </returns>
        (Func<TextReader> Open, string FilePath) Resolve(string originFilePath, string usingPath);
    }

    public class Parser
    {
        public static StringComparer CaseInsensitiveComparer => ScriptAssembly.Assembler.CaseInsensitiveComparer;

        public string FilePath { get; }
        public TextReader Input { get; }
        public IUsingResolver? UsingResolver { get; set; }
        public Program OutputAst { get; private set; }
        private readonly HashSet<string> usings = new();
        private bool scriptNameFound = false, scriptHashFound = false, argVarFound = false;

        /// <summary>
        /// Stack with the files currently being parsed.
        /// </summary>
        private readonly Stack<string> filesStack = new();
        /// <summary>
        /// Gets the file currently being parsed.
        /// </summary>
        private string CurrentFile => filesStack.Peek();

        private DiagnosticsReport? diagnostics = null;
        private DiagnosticsReport Diagnostics
        {
            get
            {
                Debug.Assert(diagnostics is not null);
                return diagnostics;
            }
        }

        public Parser(TextReader input, string fileName)
        {
            FilePath = fileName;
            Input = input;
            OutputAst = new Program(SourceRange.Unknown);
        }

        public void Parse(DiagnosticsReport diagnostics)
        {
            if (UsingResolver is not null)
            {
                // add the initial file to the set of files included with USING, in case some other file includes it recursively
                usings.Add(UsingResolver.NormalizeFilePath(FilePath));
            }
            ParseFile(Input, FilePath, diagnostics);
        }

        private void ParseFile(TextReader input, string filePath, DiagnosticsReport diagnostics)
        {
            this.diagnostics = diagnostics;
            filesStack.Push(filePath);
            
            var inputStream = new AntlrInputStream(input);
            var lexer = new ScLangLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new SyntaxErrorListener<int>(this));
            var tokens = new CommonTokenStream(lexer);
            var parser = new ScLangParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new SyntaxErrorListener<IToken>(this));

            ProcessParseTree(parser.program());

            filesStack.Pop();
        }

        private void ProcessParseTree(ScLangParser.ProgramContext context)
        {
            foreach (var directive in context.directive())
            {
                ProcessDirective(directive);
            }

            foreach (var astDecl in context.declaration().SelectMany(BuildAst))
            {
                OutputAst.Declarations.Add(astDecl);
            }
        }

        private void ProcessDirective(ScLangParser.DirectiveContext context)
        {
            switch (context)
            {
                case ScLangParser.ScriptHashDirectiveContext h:
                    if (!scriptHashFound)
                    {
                        OutputAst.ScriptHash = Parse(h.integer());
                        scriptHashFound = true;
                    }
                    else
                    {
                        Diagnostics.AddError("SCRIPT_HASH directive is repeated", Source(h));
                    }
                    break;

                case ScLangParser.ScriptNameDirectiveContext n:
                    if (!scriptNameFound)
                    {
                        OutputAst.ScriptName = n.identifier().GetText();
                        scriptNameFound = true;
                    }
                    else
                    {
                        Diagnostics.AddError("SCRIPT_NAME directive is repeated", Source(n));
                    }
                    break;

                case ScLangParser.UsingDirectiveContext u:
                    if (UsingResolver == null)
                    {
                        Diagnostics.AddWarning($"USING directive but {nameof(Parser)}.{nameof(UsingResolver)} is not set", Source(u));
                    }
                    else
                    {
                        var usingPath = Parse(u.@string());
                        var (open, newFilePath) = UsingResolver.Resolve(CurrentFile, usingPath);
                        if (usings.Add(newFilePath))
                        {
                            // merge the AST from the included file
                            using var newInput = open();
                            ParseFile(newInput, newFilePath, diagnostics!);
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
                    yield return new FuncDeclaration(Source(c), c.name.GetText(),
                                                     BuildFuncProtoDecl(Source(c), c.name.GetText() + "@proto", FuncKind.UserDefined, null, c.parameterList()))
                    {
                        Body = BuildStatementBlock(c.statementBlock())
                    };
                    break;

                case ScLangParser.FunctionDeclarationContext c:
                    yield return new FuncDeclaration(Source(c), c.name.GetText(),
                                                     BuildFuncProtoDecl(Source(c), c.name.GetText() + "@proto", FuncKind.UserDefined, c.returnType, c.parameterList()))
                    {
                        Body = BuildStatementBlock(c.statementBlock())
                    };
                    break;

                case ScLangParser.ProcedureNativeDeclarationContext c:
                    yield return new FuncDeclaration(Source(c), c.name.GetText(),
                                                     BuildFuncProtoDecl(Source(c), c.name.GetText() + "@proto", FuncKind.Native, null, c.parameterList()));
                    break;

                case ScLangParser.FunctionNativeDeclarationContext c:
                    yield return new FuncDeclaration(Source(c), c.name.GetText(),
                                                     BuildFuncProtoDecl(Source(c), c.name.GetText() + "@proto", FuncKind.Native, c.returnType, c.parameterList()));
                    break;

                case ScLangParser.ProcedurePrototypeDeclarationContext c:
                    yield return BuildFuncProtoDecl(Source(c), c.name.GetText(), FuncKind.UserDefined, null, c.parameterList());
                    break;

                case ScLangParser.FunctionPrototypeDeclarationContext c:
                    yield return BuildFuncProtoDecl(Source(c), c.name.GetText(), FuncKind.UserDefined, c.returnType, c.parameterList());
                    break;

                case ScLangParser.GlobalBlockDeclarationContext c:
                    yield return new GlobalBlockDeclaration(Source(c), c.owner.GetText(), Parse(c.block))
                    {
                        Vars = c.varDeclaration().SelectMany(decl => BuildVarDecls(VarKind.Global, decl)).ToList()
                    };
                    break;

                case ScLangParser.StructDeclarationContext c:
                    yield return new StructDeclaration(Source(c), c.identifier().GetText())
                    {
                        Fields = BuildStructFields(c.structFieldList())
                    };
                    break;

                case ScLangParser.EnumDeclarationContext c:
                    var enumDecl = new EnumDeclaration(Source(c), c.identifier().GetText());
                    enumDecl.Members = c.enumList().enumMemberDeclarationList()
                                        .SelectMany(l => l.enumMemberDeclaration())
                                        .Select(m => new EnumMemberDeclaration(Source(m), m.identifier().GetText(), enumDecl.CreateType(Source(m)), BuildAstOpt(m.initializer)))
                                        .ToList();
                    yield return enumDecl;
                    break;

                case ScLangParser.ConstantVariableDeclarationContext c:
                    foreach (var varDecl in BuildVarDecls(VarKind.Constant, c.varDeclaration()))
                    {
                        if (varDecl.Initializer is null)
                        {
                            Diagnostics.AddError("A CONST variable requires an initializer", varDecl.Source);
                        }

                        yield return varDecl;
                    }
                    break;

                case ScLangParser.ArgVariableDeclarationContext c:
                    if (!argVarFound)
                    {
                        yield return BuildSingleVarDecl(VarKind.StaticArg, c.singleVarDeclaration());
                        argVarFound = true;
                    }
                    else
                    {
                        Diagnostics.AddError("ARG variable is repeated", Source(c));
                    }
                    break;

                case ScLangParser.StaticVariableDeclarationContext c:
                    foreach (var varDecl in BuildVarDecls(VarKind.Static, c.varDeclaration()))
                    {
                        yield return varDecl;
                    }
                    break;

                default: throw new NotSupportedException($"Declaration '{context.GetType()}' is not supported");
            }
        }

        private IExpression? BuildAstOpt(ScLangParser.ExpressionContext? context)
            => context is not null ? BuildAst(context) : null;

        private IExpression BuildAst(ScLangParser.ExpressionContext context)
            => context switch
            {
                ScLangParser.ParenthesizedExpressionContext c
                    => BuildAst(c.expression()),

                ScLangParser.UnaryExpressionContext c
                    => new UnaryExpression(Source(c), UnaryOperatorExtensions.FromToken(c.op.Text), BuildAst(c.expression())),

                ScLangParser.BinaryExpressionContext c
                    => new BinaryExpression(Source(c), BinaryOperatorExtensions.FromToken(c.op.Text), BuildAst(c.left), BuildAst(c.right)),

                ScLangParser.IndexingExpressionContext c
                    => new IndexingExpression(Source(c), BuildAst(c.expression()), BuildAst(c.arrayIndexer().expression())),

                ScLangParser.InvocationExpressionContext c
                    => new InvocationExpression(Source(c), BuildAst(c.expression()), c.argumentList().expression().Select(BuildAst)),

                ScLangParser.FieldAccessExpressionContext c
                    => new FieldAccessExpression(Source(c), BuildAst(c.expression()), c.identifier().GetText()),

                ScLangParser.VectorExpressionContext c
                    => new VectorExpression(Source(c), BuildAst(c.x), BuildAst(c.y), BuildAst(c.z)),

                ScLangParser.IdentifierExpressionContext c
                    => new ValueDeclRefExpression(Source(c), c.identifier().GetText()),

                ScLangParser.IntLiteralExpressionContext c
                    => new IntLiteralExpression(Source(c), Parse(c.integer())),

                ScLangParser.FloatLiteralExpressionContext c
                    => new FloatLiteralExpression(Source(c), Parse(c.@float())),

                ScLangParser.StringLiteralExpressionContext c
                    => new StringLiteralExpression(Source(c), Parse(c.@string())),

                ScLangParser.BoolLiteralExpressionContext c
                    => new BoolLiteralExpression(Source(c), Parse(c.@bool())),

                ScLangParser.SizeOfExpressionContext c
                    => new SizeOfExpression(Source(c), BuildAst(c.expression())),

                ScLangParser.NullExpressionContext c
                    => new NullExpression(Source(c)),

                _ => throw new NotSupportedException($"Expression '{context.GetType()}' is not supported"),
            };

        private IEnumerable<IStatement> BuildAst(ScLangParser.LabeledStatementContext context)
        {
            if (context.label() is not null and var label)
            {
                yield return new LabelDeclaration(Source(label), label.identifier().GetText());
            }

            switch (context.statement())
            {
                case null: break;

                case ScLangParser.AssignmentStatementContext c:
                    var op = c.op.Text is "=" ? (BinaryOperator?)null : BinaryOperatorExtensions.FromToken(c.op.Text[0..1]);
                    yield return new AssignmentStatement(Source(c), op, BuildAst(c.left), BuildAst(c.right));
                    break;

                case ScLangParser.VariableDeclarationStatementContext c:
                    foreach (var varDecl in BuildVarDecls(VarKind.Local, c.varDeclaration()))
                    {
                        yield return varDecl;
                    }
                    break;

                case ScLangParser.IfStatementContext c:
                    var ifStmt = new IfStatement(Source(c), BuildAst(c.condition))
                    {
                        Then = BuildStatementBlock(c.thenBlock)
                    };

                    // convert ELIFs to nested IFs inside ELSE
                    var currIfStmt = ifStmt;
                    foreach (var elifBlock in c.elifBlock())
                    {
                        var innerIfStmt = new IfStatement(Source(elifBlock), BuildAst(elifBlock.condition))
                        {
                            Then = BuildStatementBlock(elifBlock.statementBlock())
                        };
                        currIfStmt.Else = new List<IStatement> { innerIfStmt };
                        currIfStmt = innerIfStmt;
                    }

                    if (c.elseBlock() is not null and var elseBlock)
                    {
                        currIfStmt.Else = BuildStatementBlock(elseBlock.statementBlock());
                    }

                    yield return ifStmt;
                    break;

                case ScLangParser.RepeatStatementContext c:
                    yield return new RepeatStatement(Source(c), BuildAst(c.limit), BuildAst(c.counter))
                    {
                        Body = BuildStatementBlock(c.statementBlock())
                    };
                    break;

                case ScLangParser.WhileStatementContext c:
                    yield return new WhileStatement(Source(c), BuildAst(c.condition))
                    {
                        Body = BuildStatementBlock(c.statementBlock())
                    };
                    break;

                case ScLangParser.SwitchStatementContext c:
                    var switchStmt = new SwitchStatement(Source(c), BuildAst(c.expression()));
                    foreach (var switchCase in c.switchCase())
                    {
                        SwitchCase switchCaseAst = switchCase switch
                        {
                            ScLangParser.ValueSwitchCaseContext v => new ValueSwitchCase(Source(v), BuildAst(v.value))
                            {
                                Body = BuildStatementBlock(v.statementBlock())
                            },
                            ScLangParser.DefaultSwitchCaseContext d => new DefaultSwitchCase(Source(d))
                            {
                                Body = BuildStatementBlock(d.statementBlock())
                            },
                            _ => throw new NotSupportedException($"Switch case '{switchCase.GetType()}' is not supported"),
                        };

                        switchStmt.Cases.Add(switchCaseAst);
                    }
                    yield return switchStmt;
                    break;

                case ScLangParser.BreakStatementContext c:
                    yield return new BreakStatement(Source(c));
                    break;

                case ScLangParser.GotoStatementContext c:
                    yield return new GotoStatement(Source(c), c.identifier().GetText());
                    break;

                case ScLangParser.ReturnStatementContext c:
                    yield return new ReturnStatement(Source(c), BuildAstOpt(c.expression()));
                    break;

                case ScLangParser.InvocationStatementContext c:
                    yield return new InvocationExpression(Source(c), BuildAst(c.expression()), c.argumentList().expression().Select(BuildAst));
                    break;

                default: throw new NotSupportedException($"Statement '{context.statement().GetType()}' is not supported");
            }
        }

        private IEnumerable<VarDeclaration> BuildVarDecls(VarKind kind, ScLangParser.VarDeclarationContext varDeclContext)
        {
            var initDecls = varDeclContext.initDeclaratorList().initDeclarator();
            return initDecls.Select(initDecl => BuildVarDecl(kind, varDeclContext.type, initDecl));
        }

        private VarDeclaration BuildSingleVarDecl(VarKind kind, ScLangParser.SingleVarDeclarationContext varDeclContext)
            => BuildVarDecl(kind, varDeclContext.type, varDeclContext.initDeclarator());

        private VarDeclaration BuildSingleVarDecl(VarKind kind, ScLangParser.SingleVarDeclarationNoInitContext varDeclContext)
            => BuildVarDecl(kind, Source(varDeclContext.declarator()), varDeclContext.type, varDeclContext.declarator(), null);

        private VarDeclaration BuildVarDecl(VarKind kind, ScLangParser.IdentifierContext type, ScLangParser.InitDeclaratorContext initDecl)
            => BuildVarDecl(kind, Source(initDecl), type, initDecl.declarator(), initDecl.initializer);

        private VarDeclaration BuildVarDecl(VarKind kind, SourceRange source, ScLangParser.IdentifierContext type, ScLangParser.DeclaratorContext decl, ScLangParser.ExpressionContext? init)
            => new(source, GetNameFromDeclarator(decl), BuildTypeFromDeclarator(type, decl, isParameter: kind is VarKind.Parameter), kind)
            {
                Initializer = BuildAstOpt(init),
            };

        private List<StructField> BuildStructFields(ScLangParser.StructFieldListContext structFieldsContext)
        {
            return new(structFieldsContext.varDeclaration()
                        .SelectMany(declNoInit => declNoInit.initDeclaratorList().initDeclarator()
                                                            .Select(initDecl => new StructField(Source(initDecl), GetNameFromDeclarator(initDecl.declarator()), BuildTypeFromDeclarator(declNoInit.type, initDecl.declarator(), isParameter: false))
                                                            {
                                                                Initializer = BuildAstOpt(initDecl.initializer),
                                                            })));
        }

        private List<IStatement> BuildStatementBlock(ScLangParser.StatementBlockContext context)
            => new(context.labeledStatement().SelectMany(BuildAst));

        private FuncProtoDeclaration BuildFuncProtoDecl(SourceRange source, string name, FuncKind kind, ScLangParser.IdentifierContext? returnType, ScLangParser.ParameterListContext paramsContext)
        {
            return new FuncProtoDeclaration(source, name, kind, returnType is null ? new VoidType(source) : BuildType(returnType))
            {
                Parameters = paramsContext.singleVarDeclarationNoInit()
                                .Select(pDecl => BuildSingleVarDecl(VarKind.Parameter, pDecl))
                                .ToList(),
            };
        }

        private IType BuildTypeFromDeclarator(ScLangParser.IdentifierContext baseTypeName, ScLangParser.DeclaratorContext declarator, bool isParameter)
        {
            var baseType = BuildType(baseTypeName);

            var allowIncompleteArrayType = isParameter; // incomplete arrays are only allowed as parameters
            var source = Source(declarator);
            IType ty = baseType;
            (ScLangParser.RefDeclaratorContext?, ScLangParser.NoRefDeclaratorContext?) pair = (declarator.refDeclarator(), declarator.noRefDeclarator());
            while (pair is not (null, ScLangParser.SimpleDeclaratorContext))
            {
                switch (pair)
                {
                    case (null, ScLangParser.ArrayDeclaratorContext) when ty is RefType:
                        return new ErrorType(source, Diagnostics, $"Array of references is not valid");
                    case (null, ScLangParser.ArrayDeclaratorContext d):
                        if (allowIncompleteArrayType && d.expression() is null)
                        {
                            ty = new IncompleteArrayType(Source(d), ty);

                            // after we already have an incomplete array type, we cannot have more arrays coming before it
                            // valid:   INT a[][10], INT a[10][10] 
                            // invalid: INT a[10][], INT a[][]
                            allowIncompleteArrayType = false;
                        }
                        else if (d.expression() is null || ty is IncompleteArrayType)
                        {
                            return new ErrorType(source, Diagnostics, $"Array is missing size expression");
                        }
                        else
                        {
                            ty = new ArrayType(Source(d), ty, BuildAst(d.expression()));
                        }

                        pair = (null, d.noRefDeclarator());
                        break;

                    case (ScLangParser.RefDeclaratorContext, null) when ty is RefType:
                        return new ErrorType(source, Diagnostics, $"Reference to reference is not valid");
                    case (ScLangParser.RefDeclaratorContext d, null):
                        ty = new RefType(Source(d), ty);
                        pair = (null, d.noRefDeclarator());
                        break;

                    case (null, ScLangParser.ParenthesizedRefDeclaratorContext d):
                        pair = (d.refDeclarator(), null);
                        break;

                    default: throw new NotImplementedException();
                };
            }

            if (isParameter && ty is IArrayType)
            {
                // arrays are passed by reference, so wrap them in a RefType when used as parameters
                ty = new RefType(ty.Source, ty);
            }

            return ty;
        }

        private IType BuildType(ScLangParser.IdentifierContext typeName)
            => new NamedType(Source(typeName), typeName.GetText());

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

        private SourceRange Source(ParserRuleContext context) => SourceRange.FromTokens(CurrentFile, context.Start, context.Stop);

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

        private static float Parse(ScLangParser.FloatContext context)
            => context.GetText().ParseAsFloat();

        private static bool Parse(ScLangParser.BoolContext context)
            => context.GetText().ToUpperInvariant() switch
            {
                "TRUE" => true,
                "FALSE" => false,
                _ => throw new ArgumentException($"Invalid bool '{context.GetText()}'", nameof(context)),
            };

        private sealed class SyntaxErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
        {
            private readonly Parser parser;

            public SyntaxErrorListener(Parser parser) => this.parser = parser;

            public void SyntaxError(TextWriter output, IRecognizer recognizer, TSymbol offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                var source = offendingSymbol is IToken t ?
                                SourceRange.FromTokens(parser.CurrentFile, t, null) :
                                new SourceRange(parser.CurrentFile,
                                                new SourceLocation(line, charPositionInLine),
                                                new SourceLocation(line, charPositionInLine));
                parser.Diagnostics.AddError(msg, source);
            }
        }
    }
}

namespace ScTools.ScriptLang;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Errors;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;

public class Parser
{
    internal const string MissingIdentifierLexeme = "<unknown>";
    internal const string MissingUsingPathLexeme = "<unknown>";
    internal const int MissingGlobalBlockIndex = -1;
    public static StringComparer CaseInsensitiveComparer => StringComparer.OrdinalIgnoreCase;

    private readonly IEnumerator<Token> tokenEnumerator;
    private readonly Queue<Token> lookAheadTokens = new();
    private bool isInsideCommaSeparatedVarDeclaration = false;
    private Token commaSeparatedVarDeclarationTypeIdentifier = default; // set when isInsideVarDeclaration is true

    public Lexer Lexer { get; }
    public DiagnosticsReport Diagnostics { get; }
    public bool IsAtEOF => Current.Kind is TokenKind.EOF;
    public bool IsAtEOS => Current.Kind is TokenKind.EOS or TokenKind.EOF;

    private Token Current { get; set; }
    private Diagnostic? LastError { get; set; }

    public Parser(Lexer lexer, DiagnosticsReport diagnostics, bool runPreprocessor = true)
    {
        Lexer = lexer;
        Diagnostics = diagnostics;
        tokenEnumerator = runPreprocessor ?
            new Preprocessor(diagnostics).Preprocess(lexer).GetEnumerator():
            lexer.GetEnumerator();
        Next();
    }

    #region Rules
    public CompilationUnit ParseCompilationUnit()
    {
        var usings = new List<UsingDirective>();
        var decls = new List<IDeclaration>();
        
        Accept(TokenKind.EOS, out _); // ignore empty lines at the beginning

        while (!IsAtEOF)
        {
            if (IsPossibleConstantDeclaration())
            {
                decls.Add(ParseConstantDeclaration());
            }
            else if (IsPossibleVarDeclaration())
            {
                decls.Add(ParseVarDeclaration(VarKind.Static, allowMultipleDeclarations: true));
                if (!isInsideCommaSeparatedVarDeclaration)
                {
                    ExpectEOS();
                }
            }
            else if (IsPossibleUsingDirective())
            {
                var @using = ParseUsingDirective();
                if (decls.Count > 0)
                {
                    UsingAfterDeclarationError(@using);
                }
                usings.Add(@using);
            }
            else if (IsPossibleEnumDeclaration())
            {
                decls.Add(ParseEnumDeclaration());
            }
            else if (IsPossibleStructDeclaration())
            {
                decls.Add(ParseStructDeclaration());
            }
            else if (IsPossibleFunctionDeclaration())
            {
                decls.Add(ParseFunctionDeclaration());
            }
            else if (IsPossibleFunctionTypeDefDeclaration())
            {
                decls.Add(ParseFunctionTypeDefDeclaration());
            }
            else if (IsPossibleNativeFunctionDeclaration())
            {
                decls.Add(ParseNativeFunctionDeclaration());
            }
            else if (IsPossibleNativeTypeDeclaration())
            {
                decls.Add(ParseNativeTypeDeclaration());
            }
            else if (IsPossibleScriptDeclaration())
            {
                decls.Add(ParseScriptDeclaration());
            }
            else if (IsPossibleGlobalBlockDeclaration())
            {
                decls.Add(ParseGlobalBlockDeclaration());
            }
            else
            {
                decls.Add(UnknownDeclarationError());
                // skip the current line
                while (!IsAtEOS) { Next(); }
                Accept(TokenKind.EOS, out _);
            }
        }

        return new(usings, decls);
    }

    public bool IsPossibleUsingDirective()
        => Peek(0).Kind is TokenKind.USING;
    public UsingDirective ParseUsingDirective()
    {
        ExpectOrMissing(TokenKind.USING, out var usingKeyword);
        ExpectOrMissing(TokenKind.String, out var pathString, () => Missing(Token.String(MissingUsingPathLexeme)));
        ExpectEOS();
        return new(usingKeyword, pathString);
    }

    public bool IsPossibleEnumDeclaration()
        => Peek(0).Kind is TokenKind.ENUM or TokenKind.STRICT_ENUM or TokenKind.HASH_ENUM;
    public EnumDeclaration ParseEnumDeclaration()
    {
        if (!ExpectEither(TokenKind.ENUM, TokenKind.STRICT_ENUM, TokenKind.HASH_ENUM, out var enumKeyword))
        {
            enumKeyword = Missing(TokenKind.ENUM);
        }
        ExpectOrMissing(TokenKind.Identifier, out var nameIdent, MissingIdentifier);
        ExpectEOS();

        var members = new List<EnumMemberDeclaration>();
        while (Peek(0).Kind is not TokenKind.ENDENUM)
        {
            members.Add(ParseEnumMember(out var lastMember));
            Accept(TokenKind.EOS, out _);

            if (lastMember)
            {
                break;
            }
        }

        ExpectOrMissing(TokenKind.ENDENUM, out var endenumKeyword);
        ExpectEOS();

        return new(enumKeyword, nameIdent, endenumKeyword, members);

        EnumMemberDeclaration ParseEnumMember(out bool last)
        {
            ExpectOrMissing(TokenKind.Identifier, out var nameIdent, MissingIdentifier);
            IExpression? initializerExpr = null;
            if (Accept(TokenKind.Equals, out var equalsToken))
            {
                initializerExpr = ParseExpression();
            }
            Accept(TokenKind.EOS, out _);
            last = !Accept(TokenKind.Comma, out _);

            return new(nameIdent, initializerExpr);
        }
    }

    public bool IsPossibleStructDeclaration()
        => Peek(0).Kind is TokenKind.STRUCT;
    public StructDeclaration ParseStructDeclaration()
    {
        ExpectOrMissing(TokenKind.STRUCT, out var structKeyword);
        ExpectOrMissing(TokenKind.Identifier, out var nameIdent, MissingIdentifier);
        ExpectEOS();

        var fields = new List<VarDeclaration>();
        while (Peek(0).Kind is not TokenKind.ENDSTRUCT)
        {
            fields.Add(ParseVarDeclaration(VarKind.Field, allowMultipleDeclarations: true));

            if (!isInsideCommaSeparatedVarDeclaration)
            {
                ExpectEOS();
            }
        }

        ExpectOrMissing(TokenKind.ENDSTRUCT, out var endstructKeyword);
        ExpectEOS();

        return new(structKeyword, nameIdent, endstructKeyword, fields);
    }

    public bool IsPossibleGlobalBlockDeclaration()
        => Peek(0).Kind is TokenKind.GLOBAL;
    public GlobalBlockDeclaration ParseGlobalBlockDeclaration()
    {
        ExpectOrMissing(TokenKind.GLOBAL, out var globalKeyword);
        ExpectOrMissing(TokenKind.Identifier, out var nameIdent, MissingIdentifier);
        ExpectOrMissing(TokenKind.Integer, out var blockIndex, () => Missing(Token.Integer(MissingGlobalBlockIndex)));
        ExpectEOS();

        var vars = new List<VarDeclaration>();
        while (Peek(0).Kind is not TokenKind.ENDGLOBAL)
        {
            vars.Add(ParseVarDeclaration(VarKind.Global, allowMultipleDeclarations: true));

            if (!isInsideCommaSeparatedVarDeclaration)
            {
                ExpectEOS();
            }
        }

        ExpectOrMissing(TokenKind.ENDGLOBAL, out var endglobalKeyword);
        ExpectEOS();

        return new(globalKeyword, nameIdent, blockIndex, endglobalKeyword, vars);
    }

    public bool IsPossibleScriptDeclaration()
        => Peek(0).Kind is TokenKind.SCRIPT;
    public ScriptDeclaration ParseScriptDeclaration()
    {
        ExpectOrMissing(TokenKind.SCRIPT, out var scriptKeyword);
        ExpectOrMissing(TokenKind.Identifier, out var nameIdent, MissingIdentifier);

        (IEnumerable<VarDeclaration> Params, Token OpenParen, Token CloseParen)? parameterList = null;
        if (Peek(0).Kind is TokenKind.OpenParen)
        {
            parameterList = ParseParameterList(isScriptParameterList: true);
        }
        ExpectEOS();

        var body = ParseBodyUntilAny(TokenKind.ENDSCRIPT);
        ExpectOrMissing(TokenKind.ENDSCRIPT, out var endscriptKeyword);
        ExpectEOS();
        return parameterList.HasValue ?
                new(scriptKeyword, nameIdent, parameterList.Value.OpenParen, parameterList.Value.CloseParen, endscriptKeyword,
                    parameterList.Value.Params, body) :
                new(scriptKeyword, nameIdent, endscriptKeyword,
                    body);
    }

    public bool IsPossibleFunctionDeclaration()
        => Peek(0).Kind is TokenKind.DEBUGONLY or TokenKind.FUNC or TokenKind.PROC;
    public FunctionDeclaration ParseFunctionDeclaration()
    {
        Token? debugonlyKeyword = null;
        if (Accept(TokenKind.DEBUGONLY, out var debugonlyKeywordTmp))
        {
            debugonlyKeyword = debugonlyKeywordTmp;
        }
        
        if (!ExpectEither(TokenKind.FUNC, TokenKind.PROC, out var procOrFuncKeyword))
        {
            procOrFuncKeyword = Missing(TokenKind.PROC);
        }

        Token nameIdent;
        TypeName? returnType;
        TokenKind expectedEndKeyword;
        if (procOrFuncKeyword.Kind is TokenKind.FUNC)
        {
            expectedEndKeyword = TokenKind.ENDFUNC;
            ExpectOrMissing(TokenKind.Identifier, out var returnTypeIdent, MissingIdentifier);
            returnType = new(returnTypeIdent);
            ExpectOrMissing(TokenKind.Identifier, out nameIdent, MissingIdentifier);
        }
        else // PROC
        {
            expectedEndKeyword = TokenKind.ENDPROC;
            returnType = null;
            ExpectOrMissing(TokenKind.Identifier, out nameIdent, MissingIdentifier);
        }

        (IEnumerable<VarDeclaration> @params, Token openParen, Token closeParen) = ParseParameterList();
        ExpectEOS();
        
        var body = ParseBodyUntilAny(TokenKind.ENDFUNC, TokenKind.ENDPROC);
        if (!ExpectOrMissing(expectedEndKeyword, out var endKeyword))
        {
            // didn't find the ENDFUNC or ENDPROC token matching the beginning FUNC or PROC,
            // but if it is any of them, skip it to continue parsing correctly
            if (Peek(0).Kind is TokenKind.ENDFUNC or TokenKind.ENDPROC)
            {
                Next();
            }
        }
        ExpectEOS();
        return new(debugonlyKeyword, procOrFuncKeyword, nameIdent, openParen, closeParen, endKeyword,
                   returnType, @params, body);
    }

    public bool IsPossibleFunctionTypeDefDeclaration()
        => Peek(0).Kind is TokenKind.TYPEDEF;
    public FunctionTypeDefDeclaration ParseFunctionTypeDefDeclaration()
    {
        ExpectOrMissing(TokenKind.TYPEDEF, out var typedefKeyword);

        if (!ExpectEither(TokenKind.FUNC, TokenKind.PROC, out var procOrFuncKeyword))
        {
            procOrFuncKeyword = Missing(TokenKind.PROC);
        }

        Token nameIdent;
        TypeName? returnType;
        if (procOrFuncKeyword.Kind is TokenKind.FUNC)
        {
            ExpectOrMissing(TokenKind.Identifier, out var returnTypeIdent, MissingIdentifier);
            returnType = new(returnTypeIdent);
            ExpectOrMissing(TokenKind.Identifier, out nameIdent, MissingIdentifier);
        }
        else // PROC
        {
            returnType = null;
            ExpectOrMissing(TokenKind.Identifier, out nameIdent, MissingIdentifier);
        }

        (IEnumerable<VarDeclaration> @params, Token openParen, Token closeParen) = ParseParameterList();
        ExpectEOS();

        return new(typedefKeyword, procOrFuncKeyword, nameIdent, openParen, closeParen, returnType, @params);
    }

    public bool IsPossibleNativeFunctionDeclaration()
        => Peek(0).Kind is TokenKind.NATIVE && Peek(1).Kind is TokenKind.DEBUGONLY or TokenKind.FUNC or TokenKind.PROC;
    public NativeFunctionDeclaration ParseNativeFunctionDeclaration()
    {
        ExpectOrMissing(TokenKind.NATIVE, out var nativeKeyword);

        Token? debugonlyKeyword = null;
        if (Accept(TokenKind.DEBUGONLY, out var debugonlyKeywordTmp))
        {
            debugonlyKeyword = debugonlyKeywordTmp;
        }

        if (!ExpectEither(TokenKind.FUNC, TokenKind.PROC, out var procOrFuncKeyword))
        {
            procOrFuncKeyword = Missing(TokenKind.PROC);
        }

        Token nameIdent;
        TypeName? returnType;
        if (procOrFuncKeyword.Kind is TokenKind.FUNC)
        {
            ExpectOrMissing(TokenKind.Identifier, out var returnTypeIdent, MissingIdentifier);
            returnType = new(returnTypeIdent);
            ExpectOrMissing(TokenKind.Identifier, out nameIdent, MissingIdentifier);
        }
        else // PROC
        {
            returnType = null;
            ExpectOrMissing(TokenKind.Identifier, out nameIdent, MissingIdentifier);
        }

        (IEnumerable<VarDeclaration> @params, Token openParen, Token closeParen) = ParseParameterList();
        IExpression? idExpr = null;
        if (Accept(TokenKind.Equals, out _))
        {
            idExpr = ParseExpression();
        }
        ExpectEOS();

        return new(nativeKeyword, debugonlyKeyword, procOrFuncKeyword, nameIdent, openParen, closeParen, returnType, @params, idExpr);
    }

    public bool IsPossibleNativeTypeDeclaration()
        => Peek(0).Kind is TokenKind.NATIVE;
    public NativeTypeDeclaration ParseNativeTypeDeclaration()
    {
        ExpectOrMissing(TokenKind.NATIVE, out var nativeKeyword);
        ExpectOrMissing(TokenKind.Identifier, out var nameIdent, MissingIdentifier);
        TypeName? baseType = null;
        if (Accept(TokenKind.Colon, out var colon))
        {
            ExpectOrMissing(TokenKind.Identifier, out var baseTypeIdent, MissingIdentifier);
            baseType = new(baseTypeIdent);
        }
        ExpectEOS();
        return new(nativeKeyword, nameIdent, baseType);
    }

    private (IEnumerable<VarDeclaration> Params, Token OpenParen, Token CloseParen) ParseParameterList(bool isScriptParameterList = false)
    {
        List<VarDeclaration>? @params = null;

        if (ExpectOrMissing(TokenKind.OpenParen, out var openParen))
        {
            if (Peek(0).Kind is not TokenKind.CloseParen)
            {
                @params = new();
                do
                {
                    @params.Add(ParseVarDeclaration(isScriptParameterList ? VarKind.ScriptParameter : VarKind.Parameter, allowMultipleDeclarations: false));
                } while (Accept(TokenKind.Comma, out _));
            }
        }
        ExpectOrMissing(TokenKind.CloseParen, out var closeParen);

        return (@params ?? Enumerable.Empty<VarDeclaration>(), openParen, closeParen);
    }

    public bool IsPossibleLabel()
        => Peek(0).Kind is TokenKind.Identifier && Peek(1).Kind is TokenKind.Colon;
    public Label? ParseLabel()
    {
        // label : identifier ':' ;

        if (Expect(TokenKind.Identifier, out var ident) &&
            Expect(TokenKind.Colon, out var colon))
        {
            return new Label(ident, colon);
        }
        else
        {
            return null;
        }
    }

    public IStatement ParseStatement()
    {
        /*  statement
            : varDeclaration                                                    #variableDeclarationStatement
            | left=expression op=('=' | '*=' | '/=' | '%=' | '+=' | '-=' | '&=' | '^=' | '|=') right=expression   #assignmentStatement

            | K_IF condition=expression EOL
              thenBlock=statementBlock
              elifBlock*
              elseBlock?
              K_ENDIF                                                   #ifStatement
    
            | K_WHILE condition=expression EOL
              statementBlock
              K_ENDWHILE                                                #whileStatement
    
            | K_REPEAT limit=expression counter=identifer EOL
              statementBlock
              K_ENDREPEAT                                               #repeatStatement
    
            | K_SWITCH expression EOL
              switchCase*
              K_ENDSWITCH                                               #switchStatement

            | K_BREAK                                                   #breakStatement
            | K_CONTINUE                                                #continueStatement
            | K_RETURN expression?                                      #returnStatement
            | K_GOTO identifier                                         #gotoStatement
            | expression argumentList                                   #invocationStatement
            ;

            labeledStatement
            : label? statement?
            ;
        */

        var label = !isInsideCommaSeparatedVarDeclaration && IsPossibleLabel() ? ParseLabel() : null;
        IStatement stmt;

        bool allowEmptyStatement = false;
        if (label is not null)
        {
            allowEmptyStatement = Accept(TokenKind.EOS, out _); // allow new-lines after the label
        }


        if (IsPossibleVarDeclaration())
        {
            stmt = ParseVarDeclaration(VarKind.Local, allowMultipleDeclarations: true).WithLabel(label);
        }
        else if (IsPossibleExpression() &&
                 !IsPossibleLabel()) // in case of sequential labels, avoid parsing the second label identifier as an expression
        {
            var lhs = ParseExpression();
            if (IsAtEOS)
            {
                stmt = lhs is InvocationExpression invocation ?
                            invocation.WithLabel(label) :
                            ExpressionAsStatementError(lhs, label);
            }
            else if (Accept(AssignmentStatement.IsAssignmentOperator, out var assignmentOp))
            {
                stmt = new AssignmentStatement(assignmentOp, lhs, rhs: ParseExpression(), label);
            }
            else
            {
                UnexpectedTokenExpectedAssignmentError();
                stmt = new ErrorStatement(LastError!, label, Current);
            }
        }
        else if (TryParseIfStatement(TokenKind.IF, out _, out var ifStmt, label))
        {
            stmt = ifStmt;
        }
        else if (Accept(TokenKind.WHILE, out var whileToken))
        {
            if (Expect(ParseExpression, out var conditionExpr) && ExpectEOS())
            {
                var body = ParseBodyUntilAny(TokenKind.ENDWHILE);
                ExpectOrMissing(TokenKind.ENDWHILE, out var endwhileToken);
                stmt = new WhileStatement(whileToken, endwhileToken, conditionExpr, body, label);
            }
            else
            {
                stmt = new ErrorStatement(LastError!, label, whileToken);
            }
        }
        else if (Accept(TokenKind.REPEAT, out var repeatToken))
        {
            if (Expect(ParseExpression, out var limitExpr) &&
                Expect(ParseExpression, out var counterExpr) &&
                ExpectEOS())
            {
                var body = ParseBodyUntilAny(TokenKind.ENDREPEAT);
                ExpectOrMissing(TokenKind.ENDREPEAT, out var endrepeatToken);
                stmt = new RepeatStatement(repeatToken, endrepeatToken, limitExpr, counterExpr, body, label);
            }
            else
            {
                stmt = new ErrorStatement(LastError!, label, repeatToken);
            }
        }
        else if (Accept(TokenKind.FOR, out var forToken))
        {
            if (Expect(ParseExpression, out var counterExpr) &&
                Expect(TokenKind.Equals, out _) &&
                Expect(ParseExpression, out var initializerExpr) &&
                Expect(TokenKind.TO, out var toToken) &&
                Expect(ParseExpression, out var limitExpr) &&
                ExpectEOS())
            {
                var body = ParseBodyUntilAny(TokenKind.ENDFOR);
                ExpectOrMissing(TokenKind.ENDFOR, out var endforToken);
                stmt = new ForStatement(forToken, toToken, endforToken, counterExpr, initializerExpr, limitExpr, body, label);
            }
            else
            {
                stmt = new ErrorStatement(LastError!, label, forToken);
            }
        }
        else if (Accept(TokenKind.SWITCH, out var switchToken))
        {
            if (Expect(ParseExpression, out var expr) && ExpectEOS())
            {
                var cases = ParseSwitchCases();
                ExpectOrMissing(TokenKind.ENDSWITCH, out var endswitchToken);
                stmt = new SwitchStatement(switchToken, endswitchToken, expr, cases, label);
            }
            else
            {
                stmt = new ErrorStatement(LastError!, label, switchToken);
            }
        }
        else if (Accept(TokenKind.BREAK, out var breakToken))
        {
            stmt = new BreakStatement(breakToken, label);
        }
        else if (Accept(TokenKind.CONTINUE, out var continueToken))
        {
            stmt = new ContinueStatement(continueToken, label);
        }
        else if (Accept(TokenKind.RETURN, out var returnToken))
        {
            stmt = new ReturnStatement(returnToken, IsAtEOS ? null : ParseExpression(), label);
        }
        else if (Accept(TokenKind.GOTO, out var gotoToken))
        {
            ExpectOrMissing(TokenKind.Identifier, out var gotoTargetToken, MissingIdentifier);
            stmt = new GotoStatement(gotoToken, gotoTargetToken, label);
        }
        else if (allowEmptyStatement)
        {
            // found a label followed by EOS but no statement afterwards so create an empty statement
            stmt = new EmptyStatement(label);
        }
        else
        {
            stmt = UnknownStatementError(label);
        }

        if (stmt is IError)
        {
            // skip this line in case of errors
            while (!IsAtEOS) { Next(); }
        }

        if (stmt is not EmptyStatement && !isInsideCommaSeparatedVarDeclaration)
        {
            ExpectEOS();
        }

        return stmt;


        bool TryParseIfStatement(TokenKind ifOrElif, out Token ifOrElifToken, [NotNullWhen(true)] out IStatement? stmt, Label? label = null)
        {
            if (Accept(ifOrElif, out ifOrElifToken))
            {
                if (Expect(ParseExpression, out var conditionExpr) && ExpectEOS())
                {
                    var thenBody = ParseBodyUntilAny(TokenKind.ELIF, TokenKind.ELSE, TokenKind.ENDIF);
                    if (TryParseIfStatement(TokenKind.ELIF, out var elifToken, out var elifStmt))
                    {
                        var elseBody = new[] { elifStmt };
                        stmt = new IfStatement(ifOrElifToken, elifToken, endifKeyword: elifStmt.Tokens.Last(), conditionExpr, thenBody, elseBody, label);
                    }
                    else if (Accept(TokenKind.ELSE, out var elseToken))
                    {
                        ExpectEOS();
                        var elseBody = ParseBodyUntilAny(TokenKind.ENDIF);
                        ExpectOrMissing(TokenKind.ENDIF, out var endifToken);
                        stmt = new IfStatement(ifOrElifToken, elseToken, endifToken, conditionExpr, thenBody, elseBody, label);
                    }
                    else
                    {
                        ExpectOrMissing(TokenKind.ENDIF, out var endifToken);
                        stmt = new IfStatement(ifOrElifToken, endifToken, conditionExpr, thenBody, label);
                    }
                }
                else
                {
                    stmt = new ErrorStatement(LastError!, label, ifOrElifToken);
                }

                return true;
            }

            stmt = null;
            return false;
        }

        List<SwitchCase> ParseSwitchCases()
        {
            var cases = new List<SwitchCase>();
            while (!IsAtEOF)
            {
                if (Peek(0).Kind is TokenKind.ENDSWITCH)
                {
                    break;
                }
                else if (Accept(TokenKind.CASE, out var caseToken))
                {
                    if (Expect(ParseExpression, out var valueExpr) && ExpectEOS())
                    {
                        var body = ParseBodyUntilAny(TokenKind.CASE, TokenKind.DEFAULT, TokenKind.ENDSWITCH);
                        cases.Add(new ValueSwitchCase(caseToken, valueExpr, body));
                        continue;
                    }
                }
                else if (Accept(TokenKind.DEFAULT, out var defaultToken))
                {
                    if (ExpectEOS())
                    {
                        var body = ParseBodyUntilAny(TokenKind.CASE, TokenKind.DEFAULT, TokenKind.ENDSWITCH);
                        cases.Add(new DefaultSwitchCase(defaultToken, body));
                        continue;
                    }
                }
                else
                {
                    UnexpectedTokenError(TokenKind.CASE, TokenKind.DEFAULT);
                }

                // if we reached here, there was an error so skip this line
                while (!IsAtEOS) { Next(); }
            }
            return cases;
        }
    }

    public bool IsPossibleExpression()
        => Peek(0).Kind is TokenKind.OpenParen or
                           TokenKind.NOT or
                           TokenKind.Minus or
                           TokenKind.LessThanLessThan or
                           TokenKind.Identifier or
                           TokenKind.Integer or
                           TokenKind.Float or
                           TokenKind.String or
                           TokenKind.Boolean or
                           TokenKind.Null;
    public IExpression ParseExpression()
    {
        /*  expression
            : '(' expression ')'                                            #parenthesizedExpression
            | expression '.' identifier                                     #fieldAccessExpression
            | expression argumentList                                       #invocationExpression
            | expression arrayIndexer                                       #indexingExpression
            | op=(K_NOT | '-') expression                                   #unaryExpression
            | left=expression op=('*' | '/' | '%') right=expression         #binaryExpression
            | left=expression op=('+' | '-') right=expression               #binaryExpression
            | left=expression op=('<' | '>' | '<=' | '>=') right=expression #binaryExpression
            | left=expression op=('==' | '<>') right=expression             #binaryExpression
            | left=expression op='&' right=expression                       #binaryExpression
            | left=expression op='^' right=expression                       #binaryExpression
            | left=expression op='|' right=expression                       #binaryExpression
            | left=expression op=K_AND right=expression                     #binaryExpression
            | left=expression op=K_OR right=expression                      #binaryExpression
            | '<<' x=expression ',' y=expression ',' z=expression '>>'      #vectorExpression
            | identifier                                                    #identifierExpression
            | integer                                                       #intLiteralExpression
            | float                                                         #floatLiteralExpression
            | string                                                        #stringLiteralExpression
            | bool                                                          #boolLiteralExpression
            | K_SIZE_OF '(' expression ')'                                  #sizeOfExpression
            | K_NULL                                                        #nullExpression
            ;
                    |
                    |   remove left-recursion and apply precedence/associativity through extra rules
                    V
            expression: binaryOp1 ;

            expressionTerm
            : ( '(' expression ')'
                | op=(K_NOT | '-') expressionTerm
                | '<<' x=expression ',' y=expression ',' z=expression '>>'
                | identifier
                | integer
                | float
                | string
                | bool
                | K_SIZE_OF '(' expression ')'
                | K_NULL
              ) expressionTermSuffix?
            ;

            expressionTermSuffix
            : ( '.' identifier
                | '(' (expression (',' expression)*)? ')'
                | '[' expression ']'
              ) expressionTermSuffix?
            ;

            binaryOp1: binaryOp2 (K_OR binaryOp2)* ;

            binaryOp2: binaryOp3 (K_AND binaryOp3)* ;

            binaryOp3: binaryOp4 ('|' binaryOp4)* ;

            binaryOp4: binaryOp5 ('^' binaryOp5)* ;

            binaryOp5: binaryOp6 ('&' binaryOp6)* ;

            binaryOp6: binaryOp7 (('==' | '<>') binaryOp7)* ;

            binaryOp7: binaryOp8 (('<' | '>' | '<=' | '>=') binaryOp8)* ;

            binaryOp8: binaryOp9 (('+' | '-') binaryOp9)* ;

            binaryOp9: expressionTerm (('*' | '/' | '%') expressionTerm)* ;
        */

        return ParseBinaryOp1();

        IExpression ParseBinaryOp1()
        {
            IExpression expr = ParseBinaryOp2();
            while (Accept(TokenKind.OR, out var orToken))
            {
                expr = new BinaryExpression(orToken, expr, ParseBinaryOp2());
            }
            return expr;
        }

        IExpression ParseBinaryOp2()
        {
            IExpression expr = ParseBinaryOp3();
            while (Accept(TokenKind.AND, out var andToken))
            {
                expr = new BinaryExpression(andToken, expr, ParseBinaryOp3());
            }
            return expr;
        }

        IExpression ParseBinaryOp3()
        {
            IExpression expr = ParseBinaryOp4();
            while (Accept(TokenKind.Bar, out var barToken))
            {
                expr = new BinaryExpression(barToken, expr, ParseBinaryOp4());
            }
            return expr;
        }

        IExpression ParseBinaryOp4()
        {
            IExpression expr = ParseBinaryOp5();
            while (Accept(TokenKind.Caret, out var caretToken))
            {
                expr = new BinaryExpression(caretToken, expr, ParseBinaryOp5());
            }
            return expr;
        }

        IExpression ParseBinaryOp5()
        {
            IExpression expr = ParseBinaryOp6();
            while (Accept(TokenKind.Ampersand, out var ampersandToken))
            {
                expr = new BinaryExpression(ampersandToken, expr, ParseBinaryOp6());
            }
            return expr;
        }

        IExpression ParseBinaryOp6()
        {
            IExpression expr = ParseBinaryOp7();
            while (Accept(TokenKind.EqualsEquals, out var opToken) ||
                   Accept(TokenKind.LessThanGreaterThan, out opToken))
            {
                expr = new BinaryExpression(opToken, expr, ParseBinaryOp7());
            }
            return expr;
        }

        IExpression ParseBinaryOp7()
        {
            IExpression expr = ParseBinaryOp8();
            while (Accept(TokenKind.LessThan, out var opToken) ||
                   Accept(TokenKind.LessThanEquals, out opToken) ||
                   Accept(TokenKind.GreaterThan, out opToken) ||
                   Accept(TokenKind.GreaterThanEquals, out opToken))
            {
                expr = new BinaryExpression(opToken, expr, ParseBinaryOp8());
            }
            return expr;
        }

        IExpression ParseBinaryOp8()
        {
            IExpression expr = ParseBinaryOp9();
            while (Accept(TokenKind.Plus, out var opToken) ||
                   Accept(TokenKind.Minus, out opToken))
            {
                expr = new BinaryExpression(opToken, expr, ParseBinaryOp9());
            }
            return expr;
        }

        IExpression ParseBinaryOp9()
        {
            IExpression expr = ParseExpressionTerm();
            while (Accept(TokenKind.Asterisk, out var opToken) ||
                   Accept(TokenKind.Slash, out opToken) ||
                   Accept(TokenKind.Percent, out opToken))
            {
                expr = new BinaryExpression(opToken, expr, ParseExpressionTerm());
            }
            return expr;
        }

        IExpression ParseExpressionTerm()
        {
            IExpression expr;
            if (Accept(TokenKind.OpenParen, out _))
            {
                Expect(ParseExpression, out var innerExpr);
                Expect(TokenKind.CloseParen, out _);
                expr = innerExpr;
            }
            else if (Accept(TokenKind.NOT, out var unaryOpToken) || Accept(TokenKind.Minus, out unaryOpToken))
            {
                expr = new UnaryExpression(unaryOpToken, ParseExpressionTerm());
            }
            else if (Accept(TokenKind.LessThanLessThan, out var vectorOpenToken))
            {
                Expect(ParseExpression, out var x);
                ExpectOrMissing(TokenKind.Comma, out var comma1);
                Expect(ParseExpression, out var y);
                ExpectOrMissing(TokenKind.Comma, out var comma2);
                Expect(ParseExpression, out var z);
                ExpectOrMissing(TokenKind.GreaterThanGreaterThan, out var vectorCloseToken);
                expr = new VectorExpression(vectorOpenToken, comma1, comma2, vectorCloseToken, x, y, z);
            }
            else if (Accept(TokenKind.Identifier, out var identToken))
            {
                expr = new NameExpression(identToken);
            }
            else if (Accept(TokenKind.Integer, out var intToken))
            {
                expr = new IntLiteralExpression(intToken);
            }
            else if (Accept(TokenKind.Float, out var floatToken))
            {
                expr = new FloatLiteralExpression(floatToken);
            }
            else if (Accept(TokenKind.String, out var stringToken))
            {
                expr = new StringLiteralExpression(stringToken);
            }
            else if (Accept(TokenKind.Boolean, out var boolToken))
            {
                expr = new BoolLiteralExpression(boolToken);
            }
            else if (Accept(TokenKind.Null, out var nullToken))
            {
                expr = new NullExpression(nullToken);
            }
            else
            {
                expr = UnknownExpressionError();
            }

            return TryParseExpressionTermSuffix(expr);
        }

        IExpression TryParseExpressionTermSuffix(IExpression expr)
        {
            IExpression? newExpr = null;
            if (Accept(TokenKind.Dot, out var dotToken))
            {
                ExpectOrMissing(TokenKind.Identifier, out var ident, MissingIdentifier);
                newExpr = new FieldAccessExpression(dotToken, ident, expr);
            }
            else if (Accept(TokenKind.OpenBracket, out var openBracket))
            {
                Expect(ParseExpression, out var indexExpr);
                ExpectOrMissing(TokenKind.CloseBracket, out var closeBracket);
                newExpr = new IndexingExpression(openBracket, closeBracket, expr, indexExpr);
            }
            else if (Accept(TokenKind.OpenParen, out var openParen))
            {
                List<IExpression>? args = null;
                if (!Accept(TokenKind.CloseParen, out var closeParen))
                {
                    while (!IsAtEOS)
                    {
                        args ??= new();
                        args.Add(ParseExpression());

                        if (Accept(TokenKind.CloseParen, out closeParen) ||
                            !Expect(TokenKind.Comma, out _))
                        {
                            break;
                        }
                    }
                }

                newExpr = new InvocationExpression(openParen, closeParen, expr, args ?? Enumerable.Empty<IExpression>());
            }

            return newExpr is null ? expr : TryParseExpressionTermSuffix(newExpr);
        }
    }

    private bool IsPossibleConstantDeclaration()
        => Peek(0).Kind is TokenKind.CONST_INT or TokenKind.CONST_FLOAT;

    private VarDeclaration ParseConstantDeclaration()
    {
        if (!ExpectEither(TokenKind.CONST_INT, TokenKind.CONST_FLOAT, out var constKeyword))
        {
            constKeyword = Missing(TokenKind.CONST_INT);
        }

        ExpectOrMissing(TokenKind.Identifier, out var nameIdent, MissingIdentifier);
        var expr = ParseExpression();
        ExpectEOS();
        return new(
            new(Token.Identifier(constKeyword.Kind is TokenKind.CONST_INT ? "INT" : "FLOAT", constKeyword.Location)),
            new VarDeclarator(nameIdent),
            VarKind.Constant,
            expr
        );
    }

    private bool IsPossibleVarDeclaration()
        => isInsideCommaSeparatedVarDeclaration ||
           Peek(0).Kind is TokenKind.Identifier && Peek(1).Kind is TokenKind.Identifier or TokenKind.Ampersand;
    private VarDeclaration ParseVarDeclaration(VarKind varKind, bool allowMultipleDeclarations)
    {
        Token typeIdent;
        VarDeclarator decl;
        if (isInsideCommaSeparatedVarDeclaration)
        {
            // continue comma-separated var declarations
            typeIdent = commaSeparatedVarDeclarationTypeIdentifier;
            decl = ParseDeclarator();
        }
        else
        {
            ExpectOrMissing(TokenKind.Identifier, out typeIdent, MissingIdentifier);
            decl = ParseDeclarator();
        }

        IExpression? initializerExpr = null;
        if (Accept(TokenKind.Equals, out var equalsToken))
        {
            initializerExpr = ParseExpression();
        }

        if (allowMultipleDeclarations && Accept(TokenKind.Comma, out _))
        {
            isInsideCommaSeparatedVarDeclaration = true;
            commaSeparatedVarDeclarationTypeIdentifier = typeIdent;
        }
        else
        {
            isInsideCommaSeparatedVarDeclaration = false;
            commaSeparatedVarDeclarationTypeIdentifier = default;
        }

        return new(new(typeIdent), decl, varKind, initializerExpr);

        VarDeclarator ParseDeclarator()
        {
            /*  declarator
                : '&'? identifier arrayLength?
                ;

                arrayLength
                : '[' expression? ']' arrayLength?
                ;
            */

            var isRef = Accept(TokenKind.Ampersand, out var ampersandToken);
            if (ExpectOrMissing(TokenKind.Identifier, out var identifier, MissingIdentifier))
            {
                List<IExpression?>? arrayLengths = null;
                Token firstOpenBracket = default, lastCloseBracket = default;
                return TryParseArrayLengths(ref arrayLengths, ref firstOpenBracket, ref lastCloseBracket)
                    ? new VarDeclarator(identifier, firstOpenBracket, lastCloseBracket, arrayLengths, isRef ? ampersandToken : null)
                    : new VarDeclarator(identifier, isRef ? ampersandToken : null);
            }

            UnknownDeclaratorError();
            return new VarDeclarator(MissingIdentifier(), isRef ? ampersandToken : null);


            bool TryParseArrayLengths(ref List<IExpression?>? ranks, ref Token firstOpenBracket, ref Token lastCloseBracket)
            {
                if (Accept(TokenKind.OpenBracket, out var openBracket))
                {
                    if (firstOpenBracket.Kind is TokenKind.Bad)
                    {
                        firstOpenBracket = openBracket;
                    }

                    IExpression? lengthExpr = null;
                    if (!Accept(TokenKind.CloseBracket, out lastCloseBracket))
                    {
                        lengthExpr = ParseExpression();
                        ExpectOrMissing(TokenKind.CloseBracket, out lastCloseBracket);
                    }

                    ranks ??= new();
                    ranks.Add(lengthExpr);

                    TryParseArrayLengths(ref ranks, ref firstOpenBracket, ref lastCloseBracket);
                }

                return ranks != null;
            }
        }
    }

    private IEnumerable<IStatement> ParseBodyUntilAny(params TokenKind[] stopTokens)
    {
        List<IStatement>? body = null;
        while (!IsAtEOF)
        {
            if (stopTokens.Contains(Peek(0).Kind))
            {
                break;
            }

            body ??= new();
            body.Add(ParseStatement());
        }
        return body ?? Enumerable.Empty<IStatement>();
    }
    #endregion Rules

    private Token Missing(Token token)
        => token with { IsMissing = true, Location = Current.Location };
    private Token Missing(TokenKind kind)
    {
        Debug.Assert(kind.HasCanonicalLexeme());
        return Missing(kind.Create());
    }
    private Token MissingIdentifier()
        => MissingIdentifier(MissingIdentifierLexeme);
    private Token MissingIdentifier(string identifier)
        => Missing(Token.Identifier(identifier));

    private void Error(ErrorCode code, string message, SourceRange location)
        => LastError = Diagnostics.Add((int)code, DiagnosticTag.Error, message, location);

    private void UnexpectedTokenError(TokenKind expectedToken)
        => Error(ErrorCode.ParserUnexpectedToken, $"Unexpected token '{Current.Kind}', expected '{expectedToken}'", Current.Location);

    private void UnexpectedTokenError(TokenKind expectedTokenA, TokenKind expectedTokenB)
        => Error(ErrorCode.ParserUnexpectedToken, $"Unexpected token '{Current.Kind}', expected '{expectedTokenA}' or '{expectedTokenB}'", Current.Location);

    private void UnexpectedTokenError(TokenKind expectedTokenA, TokenKind expectedTokenB, TokenKind expectedTokenC)
        => Error(ErrorCode.ParserUnexpectedToken, $"Unexpected token '{Current.Kind}', expected '{expectedTokenA}', '{expectedTokenB}' or '{expectedTokenC}'", Current.Location);

    private void UnexpectedTokenExpectedAssignmentError()
        => Error(ErrorCode.ParserUnexpectedToken, $"Unexpected token '{Current.Kind}', expected assignment operator", Current.Location);

    private ErrorDeclaration UnknownDeclarationError()
    {
        Error(ErrorCode.ParserUnknownDeclaration, $"Expected declaration, found '{Current.Lexeme}' ({Current.Kind})", Current.Location);
        return new(LastError!, Current);
    }

    private ErrorStatement UnknownStatementError(Label? label)
    { 
        Error(ErrorCode.ParserUnknownStatement, $"Expected statement, found '{Current.Lexeme}' ({Current.Kind})", Current.Location);
        return new(LastError!, label, Current);
    }

    private ErrorStatement ExpressionAsStatementError(IExpression parsedExpr, Label? label)
    {
        Error(ErrorCode.ParserExpressionAsStatement, $"Only invocation expressions can be used as a statement, found '{parsedExpr.GetType().Name}'", parsedExpr.Location);
        return new(LastError!, label, parsedExpr.Tokens.ToArray());
    }

    private ErrorExpression UnknownExpressionError()
    {
        Error(ErrorCode.ParserUnknownExpression, $"Expected expression, found '{Current.Lexeme}' ({Current.Kind})", Current.Location);
        return new(LastError!, Current);
    }

    private void UnknownDeclaratorError()
        => Error(ErrorCode.ParserUnknownDeclarator, $"Expected declarator, found '{Current.Lexeme}' ({Current.Kind})", Current.Location);

    private void UsingAfterDeclarationError(UsingDirective @using)
        => Error(ErrorCode.ParserUsingAfterDeclaration, $"USING directives must precede all declarations", @using.Location);

    private Token Peek(int offset)
    {
        Debug.Assert(offset >= 0);

        if (offset == 0)
        {
            return Current;
        }

        if (lookAheadTokens.Count < offset)
        {
            while (lookAheadTokens.Count < offset)
            {
                tokenEnumerator.MoveNext();
                lookAheadTokens.Enqueue(tokenEnumerator.Current);
            }
        }
        return lookAheadTokens.ElementAt(offset - 1);
    }

    private void Next()
    {
        if (lookAheadTokens.TryDequeue(out var nextToken))
        {
            Current = nextToken;
        }
        else
        {
            tokenEnumerator.MoveNext();
            Current = tokenEnumerator.Current;
        }
    }

    private bool Accept(TokenKind token, out Token t)
    {
        t = Current;
        if (Current.Kind == token)
        {
            Next();
            return true;
        }

        return false;
    }

    private bool Accept(Predicate<TokenKind> tokenPredicate, out Token t)
    {
        t = Current;
        if (tokenPredicate(Current.Kind))
        {
            Next();
            return true;
        }

        return false;
    }

    private bool Expect(TokenKind token, out Token t)
    {
        if (Accept(token, out t))
        {
            return true;
        }

        UnexpectedTokenError(token);
        return false;
    }

    private bool ExpectOrMissing(TokenKind token, out Token t, Func<Token>? createMissingToken = null)
    {
        if (!Expect(token, out t))
        {
            t = createMissingToken is not null ? createMissingToken() : Missing(token);
            return false;
        }
        return true;
    }

    private bool Expect<TNode>(Func<TNode> parseFunc, out TNode node) where TNode : INode
    {
        node = parseFunc();
        return node is not IError;
    }

    private bool ExpectEOS() => ExpectEither(TokenKind.EOS, TokenKind.EOF, out _);

    private bool ExpectEither(TokenKind tokenA, TokenKind tokenB, out Token t)
    {
        if (Accept(tokenA, out t) || Accept(tokenB, out t))
        {
            return true;
        }

        UnexpectedTokenError(tokenA, tokenB);
        return false;
    }
    private bool ExpectEither(TokenKind tokenA, TokenKind tokenB, TokenKind tokenC, out Token t)
    {
        if (Accept(tokenA, out t) || Accept(tokenB, out t) || Accept(tokenC, out t))
        {
            return true;
        }

        UnexpectedTokenError(tokenA, tokenB, tokenC);
        return false;
    }
}

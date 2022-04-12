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
using ScTools.ScriptLang.Ast.Types;

public class ParserNew
{
    public static StringComparer CaseInsensitiveComparer => ScriptAssembly.Assembler.CaseInsensitiveComparer;

    private readonly Lexer.Enumerator lexerEnumerator;
    private readonly Queue<Token> lookAheadTokens = new();
    private bool isInsideCommaSeparatedVarDeclaration = false;
    private Token commaSeparatedVarDeclarationTypeIdentifier = default; // set when isInsideVarDeclaration is true

    public Lexer Lexer { get; }
    public DiagnosticsReport Diagnostics { get; }
    public bool IsAtEOF => Current.Kind is TokenKind.EOF;
    public bool IsAtEOS => Current.Kind is TokenKind.EOS or TokenKind.EOF;

    private Token Current { get; set; }
    private Diagnostic LastError { get; set; }

    public ParserNew(Lexer lexer, DiagnosticsReport diagnostics)
    {
        Lexer = lexer;
        Diagnostics = diagnostics;
        lexerEnumerator = lexer.GetEnumerator();
        Next();
    }


    #region Rules
    public bool IsPossibleLabel()
        => Peek(0).Kind is TokenKind.Identifier && Peek(1).Kind is TokenKind.Colon;
    public string? ParseLabel()
    {
        // label : identifier ':' ;

        if (Expect(TokenKind.Identifier, out var ident) &&
            Expect(TokenKind.Colon, out var colon))
        {
            while (Accept(TokenKind.EOS, out _)) { } // ignore any new lines/EOS after a label
            return ident.Lexeme.ToString();
        }
        else
        {
            return null;
        }
    }

    public IStatement ParseLabeledStatement()
    {
        var label = !isInsideCommaSeparatedVarDeclaration && IsPossibleLabel() ? ParseLabel() : null;
        var stmt = ParseStatement();
        stmt.Label = label;
        return stmt;
    }

    public IStatement ParseStatement()
    {
        IStatement stmt;
        
        if (IsPossibleVarDeclaration())
        {
            stmt = ParseVarDeclaration(VarKind.Local, allowMultipleDeclarations: true);
        }
        else if (Accept(TokenKind.BREAK, out var breakToken))
        {
            stmt = new BreakStatement(breakToken);
        }
        else if (Accept(TokenKind.CONTINUE, out var continueToken))
        {
            stmt = new ContinueStatement(continueToken);
        }
        else if (Accept(TokenKind.RETURN, out var returnToken))
        {
            stmt = new ReturnStatement(returnToken, IsAtEOS ? null : ParseExpression());
        }
        else if (Accept(TokenKind.GOTO, out var gotoToken))
        {
            stmt = Expect(TokenKind.Identifier, out var gotoTargetToken) ?
                        new GotoStatement(gotoToken, gotoTargetToken) :
                        new ErrorStatement(LastError, gotoToken, gotoTargetToken);
        }
        else
        {
            stmt = UnknownStatementError();
        }

        ExpectEOS();
        return stmt;
    }

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
            | left=expression op='&' right=expression                       #binaryExpression
            | left=expression op='^' right=expression                       #binaryExpression
            | left=expression op='|' right=expression                       #binaryExpression
            | left=expression op=('<' | '>' | '<=' | '>=') right=expression #binaryExpression
            | left=expression op=('==' | '<>') right=expression             #binaryExpression
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
                    |   remove left-recursion and apply precedence/associativity through rules
                    V
            expression
            : ( '(' expression ')'
                | op=(K_NOT | '-') expression
                | '<<' x=expression ',' y=expression ',' z=expression '>>'
                | identifier
                | integer
                | float
                | string
                | bool
                | K_SIZE_OF '(' expression ')'
                | K_NULL
              ) expressionAlt?
            ;

            expressionAlt
            : ( '.' identifier
                | argumentList
                | arrayIndexer
              ) expressionAlt?
            ;
        */

        /* TODO: precedence and associativiy
            | left=expression op=('*' | '/' | '%') right=expression        
            | left=expression op=('+' | '-') right=expression              
            | left=expression op='&' right=expression                      
            | left=expression op='^' right=expression                      
            | left=expression op='|' right=expression                      
            | left=expression op=('<' | '>' | '<=' | '>=') right=expression
            | left=expression op=('==' | '<>') right=expression            
            | left=expression op=K_AND right=expression                    
            | left=expression op=K_OR right=expression    
         */

        // TODO: handle expression rule with left-recursion
        IExpression expr;
        if (Accept(TokenKind.OpenParen, out var openParenToken))
        {
            expr = Expect(ParseExpression, out var innerExpr) &&
                   Expect(TokenKind.CloseParen, out var closeParentToken) ?
                        innerExpr :
                        new ErrorExpression(LastError, openParenToken);
        }
        else if (Accept(TokenKind.NOT, out var unaryOpToken) || Accept(TokenKind.Minus, out unaryOpToken))
        {
            expr = new UnaryExpression(unaryOpToken, ParseExpression());
        }
        else if (Accept(TokenKind.LessThanLessThan, out var vectorOpenToken))
        {
            expr = Expect(ParseExpression, out var x) &&
                   Expect(TokenKind.Comma, out var comma1) &&
                   Expect(ParseExpression, out var y) &&
                   Expect(TokenKind.Comma, out var comma2) &&
                   Expect(ParseExpression, out var z) &&
                   Expect(TokenKind.GreaterThanGreaterThan, out var vectorCloseToken) ?
                        new VectorExpression(vectorOpenToken, comma1, comma2, vectorCloseToken, x, y, z) :
                        new ErrorExpression(LastError, vectorOpenToken);
        }
        else if (Accept(TokenKind.Identifier, out var identToken))
        {
            expr = new DeclarationRefExpression(identToken);
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
        else if (Accept(TokenKind.SIZE_OF, out var sizeOfToken))
        {
            expr = Expect(TokenKind.OpenParen, out var sizeOfOpenToken) &&
                   Expect(ParseExpression, out var sizeOfExpr) &&
                   Expect(TokenKind.CloseParen, out var sizeOfCloseToken) ?
                        new SizeOfExpression(sizeOfToken, sizeOfOpenToken, sizeOfCloseToken, sizeOfExpr) :
                        new ErrorExpression(LastError, sizeOfToken);
        }
        else if (Accept(TokenKind.Null, out var nullToken))
        {
            expr = new NullExpression(nullToken);
        }
        else
        {
            expr = UnknownExpressionError();
        }

        return TryParseAlt(expr);

        IExpression TryParseAlt(IExpression expr)
        {
            IExpression? alt = null;
            if (Accept(TokenKind.Dot, out var dotToken))
            {
                alt = Expect(TokenKind.Identifier, out var ident) ?
                        new FieldAccessExpression(dotToken, ident, expr) :
                        new ErrorExpression(LastError, dotToken);
            }

            return alt is null ? expr : TryParseAlt(alt);
        }
    }

    private bool IsPossibleVarDeclaration()
        => isInsideCommaSeparatedVarDeclaration ||
           Peek(0).Kind is TokenKind.Identifier && Peek(1).Kind is TokenKind.Identifier or TokenKind.Ampersand;
    private VarDeclaration ParseVarDeclaration(VarKind varKind, bool allowMultipleDeclarations)
    {
        Token typeIdent;
        Declarator decl;
        if (isInsideCommaSeparatedVarDeclaration)
        {
            // continue comma-separated var declarations
            typeIdent = commaSeparatedVarDeclarationTypeIdentifier;
            decl = ParseDeclarator();
        }
        else
        {
            Expect(TokenKind.Identifier, out typeIdent);
            decl = ParseDeclarator();
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

        return new VarDeclaration(
            decl.Identifier,
            TypeFromDeclarator(decl, new NamedType(typeIdent)),
            varKind,
            isReference: decl is RefDeclarator);
    }

    #region Declarators
    private record struct DeclaratorArrayRank(Token OpenBracket, Token CloseBracket, IExpression? LengthExpression);
    private abstract record Declarator(Token Identifier);
    private record RefDeclarator(Token Ampersand, Token Identifier) : Declarator(Identifier);
    /// <param name="ArrayRanks">List of rank specifiers from left to right.</param>
    private record SimpleDeclarator(Token Identifier, List<DeclaratorArrayRank>? ArrayRanks) : Declarator(Identifier);

    private Declarator ParseDeclarator()
    {
        /*  declarator
            : identifier arrayRank?       #simpleDeclarator
            | '&' identifier              #refDeclarator
            ;

            arrayRank
            : '[' expression? ']' arrayRank?
            ;

            NOTE: arrays are passed by reference so no need to support stuff like 'INT (&arr)[10]' in the grammar, 'INT arr[10]' is equivalent
        */

        List<DeclaratorArrayRank>? arrayRanks = null;
        if (Accept(TokenKind.Identifier, out var identifier))
        {
            TryParseArrayRank(ref arrayRanks);
            return new SimpleDeclarator(identifier, arrayRanks);
        }
        else if (Accept(TokenKind.Ampersand, out var ampersandToken) &&
                 Expect(TokenKind.Identifier, out identifier))
        {
            return new RefDeclarator(ampersandToken, identifier);
        }

        UnknownDeclaratorError();
        return new SimpleDeclarator(new() { Kind = TokenKind.Identifier, Lexeme = "<unknown>".AsMemory(), Location = Current.Location }, arrayRanks);


        void TryParseArrayRank(ref List<DeclaratorArrayRank>? ranks)
        {
            if (Accept(TokenKind.OpenBracket, out var openBracket))
            {
                IExpression? lengthExpr = null;
                _ = Accept(TokenKind.CloseBracket, out var closeBracket) ||
                    Expect<IExpression>(ParseExpression, out lengthExpr) && Expect(TokenKind.CloseBracket, out closeBracket);

                ranks ??= new();
                ranks.Add(new(openBracket, closeBracket, lengthExpr));
                TryParseArrayRank(ref ranks);
            }
        }
    }

    private IType TypeFromDeclarator(Declarator declarator, IType baseType)
        => declarator switch
        {
            SimpleDeclarator { ArrayRanks: not null } s =>
                 s.ArrayRanks.Reverse<DeclaratorArrayRank>()
                             .Aggregate(baseType, (IType ty, DeclaratorArrayRank rank)
                                => rank.LengthExpression == null ?
                                    new IncompleteArrayType(rank.OpenBracket, rank.CloseBracket, ty) :
                                    new ArrayType_New(rank.OpenBracket, rank.CloseBracket, ty, rank.LengthExpression)),
            _ => baseType,
        };


    #endregion Declarators

    #endregion Rules


    private void Error(ErrorCode code, string message, SourceRange location)
        => LastError = Diagnostics.Add((int)code, DiagnosticTag.Error, message, location);

    private void UnexpectedTokenError(TokenKind expectedToken)
        => Error(ErrorCode.ParserUnexpectedToken, $"Unexpected token '{Current.Kind}', expected '{expectedToken}'", Current.Location);

    private void UnexpectedTokenError(TokenKind expectedTokenA, TokenKind expectedTokenB)
        => Error(ErrorCode.ParserUnexpectedToken, $"Unexpected token '{Current.Kind}', expected '{expectedTokenA}' or '{expectedTokenB}'", Current.Location);

    private ErrorStatement UnknownStatementError()
    { 
        Error(ErrorCode.ParserUnknownStatement, $"Expected statement, found '{Current.Lexeme}' ({Current.Kind})", Current.Location);
        return new ErrorStatement(LastError, Current);
    }

    private ErrorExpression UnknownExpressionError()
    {
        Error(ErrorCode.ParserUnknownExpression, $"Expected expression, found '{Current.Lexeme}' ({Current.Kind})", Current.Location);
        return new ErrorExpression(LastError, Current);
    }

    private void UnknownDeclaratorError()
        => Error(ErrorCode.ParserUnknownDeclarator, $"Expected declarator, found '{Current.Lexeme}' ({Current.Kind})", Current.Location);

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
                lexerEnumerator.MoveNext();
                lookAheadTokens.Enqueue(lexerEnumerator.Current);
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
            lexerEnumerator.MoveNext();
            Current = lexerEnumerator.Current;
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

    private bool Expect(TokenKind token, out Token t)
    {
        if (Accept(token, out t))
        {
            return true;
        }

        UnexpectedTokenError(token);
        return false;
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
}
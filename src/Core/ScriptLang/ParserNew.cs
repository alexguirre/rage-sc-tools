namespace ScTools.ScriptLang;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Errors;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;

public class ParserNew
{
    private readonly Lexer.Enumerator lexerEnumerator;
    private readonly Queue<Token> lookAheadTokens = new();

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
        var label = IsPossibleLabel() ? ParseLabel() : null;
        var stmt = ParseStatement();
        stmt.Label = label;
        return stmt;
    }

    public IStatement ParseStatement()
    {
        IStatement stmt;
        if (Accept(TokenKind.BREAK, out var breakToken))
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

        return expr;
    }

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
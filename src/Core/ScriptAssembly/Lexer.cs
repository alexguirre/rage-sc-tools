namespace ScTools.ScriptAssembly;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// TODO: move common code between ScriptLang.Lexer and ScriptAssembly.Lexer to a base class
public sealed class Lexer : IEnumerable<Token>
{
    public string FilePath { get; }
    public string Source { get; }
    public DiagnosticsReport Diagnostics { get; }

    public Lexer(string filePath, string source, DiagnosticsReport diagnostics)
    {
        FilePath = filePath;
        Source = NormalizeSource(source);
        Diagnostics = diagnostics;
    }

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<Token> IEnumerable<Token>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public sealed class Enumerator : IEnumerator<Token>
    {
        public Lexer Lexer { get; }
        private int startPos, endPos;
        private int startLine, endLine;
        private int startColumn, endColumn;

        public Token Current { get; private set; }

        object IEnumerator.Current => Current;

        private SourceRange TokenLocation
            => new((startLine, startColumn, Lexer.FilePath), (endLine, endColumn - 1, Lexer.FilePath));
        private ReadOnlyMemory<char> TokenLexeme
            => Lexer.Source.AsMemory(startPos, endPos - startPos);

        public Enumerator(Lexer lexer)
        {
            Lexer = lexer;
            Reset();
        }

        public void Dispose() { }

        public bool MoveNext()
        {
            if (Current.Kind is TokenKind.EOF)
            {
                return false;
            }

            Current = NextToken();
            return true;
        }

        public void Reset()
        {
            startPos = endPos = 0;
            startLine = endLine = 1;
            startColumn = endColumn = 1;
        }

        private void Error(ErrorCode code, string message, SourceRange location)
            => Lexer.Diagnostics.Add((int)code, DiagnosticTag.Error, message, location);

        private Token BadToken(ErrorCode code, string message)
        {
            Error(code, message, TokenLocation);
            return NewToken(TokenKind.Bad);
        }

        private Token UnexpectedCharacterBadToken()
            => BadToken(ErrorCode.LexerUnexpectedCharacter, $"Unexpected character '{TokenLexeme}'");

        private Token IncompleteStringBadToken()
            => BadToken(ErrorCode.LexerIncompleteString, "Incomplete string");

        private Token InvalidIntegerLiteralBadToken()
            => BadToken(ErrorCode.LexerInvalidIntegerLiteral, "Invalid integer literal");

        private Token InvalidFloatLiteralBadToken()
            => BadToken(ErrorCode.LexerInvalidFloatLiteral, "Invalid float literal");

        private Token NextToken()
        {
            SkipAllWhiteSpaces();
            Drop(); // start a new token

            var token = Next() switch
            {
                '.' => IsDecimalDigit(Peek()) ? LexFloatAfterDot() : NewToken(TokenKind.Dot),
                ',' => NewToken(TokenKind.Comma),
                '(' => NewToken(TokenKind.OpenParen),
                ')' => NewToken(TokenKind.CloseParen),
                ':' => NewToken(TokenKind.Colon),
                '"' or '\'' => LexString(),
                '`' => LexHashString(),
                '\n' => LexEOS(),
                '-' or '+' => IsDecimalDigit(Peek()) ? LexNumberWithSign() : UnexpectedCharacterBadToken(),
                >= '0' and <= '9' => LexNumber(),
                char c when IsIdentifierStartChar(c) => LexIdentifier(),

                EOF => NewToken(TokenKind.EOF, SourceRange.EOF(Lexer.FilePath)),
                _ => UnexpectedCharacterBadToken()
            };

            return token;
        }

        // NOTE: Lex* methods assume that the first character of the token was already consumed (i.e. Next() was already called for the first char)

        /// <summary>
        /// Lexes single-quote or double-quote strings.
        /// </summary>
        private Token LexString()
        {
            var quote = Peek(-1);
            Debug.Assert(quote is '"' or '\'');
            return LexStringLikeToken(TokenKind.String, quote);
        }

        /// <summary>
        /// Lexes hashed strings.
        /// </summary>
        private Token LexHashString()
        {
            Debug.Assert(Peek(-1) is '`');
            return LexStringLikeToken(TokenKind.Integer, '`');
        }

        private Token LexStringLikeToken(TokenKind kind, char delimiter)
        {
            while (true)
            {
                var curr = Peek();
                if (curr == EOF || curr == '\n')
                {
                    return IncompleteStringBadToken();
                }

                if (curr == '\\') // handle escape sequences
                {
                    var next = Peek(1);
                    var isValidEscapeSequence = next is 'n' or 'r' or 't' or '0' or '\\' or '"' or '\'' or '`';
                    if (isValidEscapeSequence)
                    {
                        Next();
                        Next();
                        continue;
                    }
                    else
                    {
                        Error(ErrorCode.LexerUnrecognizedEscapeSequence, "Unrecognized escape sequence", 
                            new((endLine, endColumn, Lexer.FilePath), (endLine, endColumn + 1, Lexer.FilePath)));
                        // continue treating this token as a string instead of returning early as a Bad token
                    }
                }

                Next();
                if (curr == delimiter)
                {
                    break;
                }
            }

            return NewToken(kind);
        }
        
        private Token LexNumberWithSign()
        {
            Debug.Assert(Peek(-1) is '-' or '+');
            Debug.Assert(Peek(0) is >= '0' and <= '9');

            Next();
            return LexNumber();
        }

        private Token LexNumber()
        {
            Debug.Assert(Peek(-1) is >= '0' and <= '9');

            var intToken = LexInteger();
            if (Peek() is '.' && IsDecimalDigit(Peek(1)))
            {
                Next(); // skip '.'
                return LexFloatAfterDot();
            }
            else
            {
                return TryLexFloatExponent() ?? intToken;
            }
        }

        private Token LexInteger()
        {
            Debug.Assert(Peek(-1) is >= '0' and <= '9');

            if (Peek(-1) == '0' && Peek(0) == 'x') // hexadecimal prefix
            {
                Next(); // skip 'x'
                if (NextIf(IsHexadecimalDigit))
                {
                    NextWhile(IsHexadecimalDigit);
                }
                else
                {
                    // found only '0x'
                    return InvalidIntegerLiteralBadToken();
                }
            }
            else // decimal
            {
                NextWhile(IsDecimalDigit);
            }

            return NewToken(TokenKind.Integer);
        }

        private Token? TryLexFloatExponent()
        {
            if (!NextIf(c => c is 'e' or 'E')) // no exponent
            {
                return null;
            }

            NextIf(c => c is '+' or '-'); // optional +/- symbol
            if (NextIf(IsDecimalDigit)) // exponent value
            {
                NextWhile(IsDecimalDigit);
                // found exponent, finish token
                return NewToken(TokenKind.Float);
            }
            else
            {
                // incomplete exponent, e.g. 1e, 1e+, 1e-
                return InvalidFloatLiteralBadToken();
            }
        }

        private Token LexFloatAfterDot()
        {
            Debug.Assert(Peek(-1) is '.');
            Debug.Assert(IsDecimalDigit(Peek(0)));

            NextWhile(IsDecimalDigit);
            return TryLexFloatExponent() ?? NewToken(TokenKind.Float);
        }

        private Token LexIdentifier()
        {
            Debug.Assert(IsIdentifierStartChar(Peek(-1)));
            NextWhile(IsIdentifierChar);

            return NewToken(TokenKind.Identifier);
        }

        /// <summary>
        /// Merges multiple sequential empty lines in a single EOS token.
        /// </summary>
        private Token LexEOS()
        {
            Debug.Assert(Peek(-1) == '\n');

            // save the original EOS start position because SkipAllWhiteSpaces drops the current token
            var eosStartPos = startPos;
            var eosStartLine = startLine;
            var eosStartColumn = startColumn;

            // skip whitespace/comments after the new line if any, so multiple lines with no tokens are merged in a single EOS token
            SkipAllWhiteSpaces();
            while (Peek() == '\n')
            {
                Next(dropTokenOnNewLine: false);
                SkipAllWhiteSpaces();
            }

            startPos = eosStartPos;
            startLine = eosStartLine;
            startColumn = eosStartColumn;
            return NewToken(TokenKind.EOS);
        }

        private void SkipAllWhiteSpaces()
        {
            Drop();
            while (SkipWhiteSpace() ||
                   SkipSingleLineComment() ||
                   SkipMultiLineComment() ||
                   SkipStatementContinuation())
            {
                Drop();
            }
        }

        private bool SkipWhiteSpace()
        {
            if (!IsWhiteSpaceChar(Peek()))
            {
                return false;
            }

            NextWhile(IsWhiteSpaceChar);
            return true;
        }

        private bool SkipSingleLineComment()
        {
            if ((Peek(0) != '/' || Peek(1) != '/') && (Peek(0) != ';'))
            {
                return false;
            }

            // skip comment prefix
            if (Peek(0) == ';')
            {
                Next();
            }
            else
            {
                Next();
                Next();
            }

            var prevIsBackslash = false;
            while (true)
            {
                var c = Peek();
                if (c == EOF ||
                    c == '\n' && !prevIsBackslash) // found new line without statement-continuation, end the comment
                {
                    break;
                }

                prevIsBackslash = c == '\\';
                Next();
            }

            return true;
        }

        private bool SkipMultiLineComment()
        {
            if (Peek(0) != '/' || Peek(1) != '*')
            {
                return false;
            }

            // skip comment prefix
            Next();
            Next();

            var prevIsAsterisk = false;
            while (true)
            {
                var c = Peek();
                if (c == EOF)
                {
                    Error(ErrorCode.LexerOpenComment, "Reached end-of-file without closing the comment, expected '*/'", TokenLocation);
                    break;
                }

                if (prevIsAsterisk && c == '/') // found comment closing delimiter
                {
                    Next(); // skip '/'
                    break;
                }

                prevIsAsterisk = c == '*';
                Next(dropTokenOnNewLine: false); // don't drop token on new lines to have the correct TokenLocation in the LexerOpenComment error
            }

            return true;
        }

        private bool SkipStatementContinuation()
        {
            if (Peek(0) != '\\' || Peek(1) != '\n')
            {
                return false;
            }

            Next();
            Next();
            return true;
        }

        private Token NewToken(TokenKind kind, SourceRange? loc = null) => new(kind, TokenLexeme, loc ?? TokenLocation);

        private void Drop()
        {
            startPos = endPos;
            startLine = endLine;
            startColumn = endColumn;
        }

        private char Peek(int offset = 0)
        {
            var peekPos = endPos + offset;
            return peekPos >= 0 && peekPos < Lexer.Source.Length ? Lexer.Source[peekPos] : EOF;
        }

        private char Next(bool dropTokenOnNewLine = true)
        {
            if (endPos < Lexer.Source.Length)
            {
                if (Peek(-1) == '\n')
                {
                    // starting new line
                    endLine++;
                    endColumn = 1;
                    if (dropTokenOnNewLine) Drop(); // start a new token
                }

                // continuing current line
                endColumn++;
                return Lexer.Source[endPos++];
            }

            return EOF;
        }

        private bool NextIf(char expected)
        {
            if (Peek() == expected)
            {
                Next();
                return true;
            }

            return false;
        }

        private bool NextIf(Predicate<char> isExpected)
        {
            if (isExpected(Peek()))
            {
                Next();
                return true;
            }

            return false;
        }

        private void NextWhile(Predicate<char> isExpected)
        {
            while (NextIf(isExpected)) { /*empty*/ }
        }
    }

    private const char EOF = char.MaxValue;

    private static string NormalizeSource(string source)
        => source.ReplaceLineEndings("\n");

    private static bool IsWhiteSpaceChar(char c)
        => c is not ('\n' or EOF) && char.IsWhiteSpace(c);
    private static bool IsIdentifierStartChar(char c)
        => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_';
    private static bool IsIdentifierChar(char c)
        => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9');
    private static bool IsDecimalDigit(char c)
        => c is >= '0' and <= '9';
    private static bool IsHexadecimalDigit(char c)
        => c is (>= '0' and <= '9') or (>= 'A' and <= 'F') or (>= 'a' and <= 'f');
}

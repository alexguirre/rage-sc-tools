namespace ScTools;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public interface IToken<TSelf, TTokenKind> where TTokenKind : struct, Enum
{
    TTokenKind Kind { get; }
    bool IsMissing { get; }
    ReadOnlyMemory<char> Lexeme { get; }
    SourceRange Location { get; }
}

public interface ILexer<TToken, TTokenKind, TErrorCode> : IEnumerable<TToken>
    where TToken : struct, IToken<TToken, TTokenKind>
    where TTokenKind : struct, Enum
    where TErrorCode : struct, Enum
{
    string FilePath { get; }
    string Source { get; }
    DiagnosticsReport Diagnostics { get; }
}

public abstract class LexerBase<TToken, TTokenKind, TErrorCode> : ILexer<TToken, TTokenKind, TErrorCode>
    where TToken : struct, IToken<TToken, TTokenKind>
    where TTokenKind : struct, Enum
    where TErrorCode : struct, Enum
{
    public string FilePath { get; }
    public string Source { get; }
    public DiagnosticsReport Diagnostics { get; }

    public LexerBase(string filePath, string source, DiagnosticsReport diagnostics)
    {
        FilePath = filePath;
        Source = NormalizeSource(source);
        Diagnostics = diagnostics;
    }

    public abstract EnumeratorBase GetEnumerator();
    IEnumerator<TToken> IEnumerable<TToken>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected const char EOF = char.MaxValue;
    private static string NormalizeSource(string source)
        => source.ReplaceLineEndings("\n");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TokenKindEquals(TTokenKind a, TTokenKind b)
        => EqualityComparer<TTokenKind>.Default.Equals(a, b);

    protected static bool IsWhiteSpaceChar(char c)
    => c is not ('\n' or EOF) && char.IsWhiteSpace(c);
    protected static bool IsDecimalDigit(char c)
        => c is >= '0' and <= '9';
    protected static bool IsHexadecimalDigit(char c)
        => c is (>= '0' and <= '9') or (>= 'A' and <= 'F') or (>= 'a' and <= 'f');

    public readonly record struct CommonTokenSet(
        TTokenKind Bad,
        TTokenKind Dot,
        TTokenKind Comma,
        TTokenKind OpenParen,
        TTokenKind CloseParen,
        TTokenKind Colon,
        TTokenKind Hash,
        TTokenKind Integer,
        TTokenKind Float,
        TTokenKind String,
        TTokenKind EOS,
        TTokenKind EOF);

    public readonly record struct CommonErrorSet(
        TErrorCode UnexpectedCharacter,
        TErrorCode IncompleteString,
        TErrorCode UnrecognizedEscapeSequence,
        TErrorCode OpenComment,
        TErrorCode InvalidIntegerLiteral,
        TErrorCode InvalidFloatLiteral);

    public abstract class EnumeratorBase : IEnumerator<TToken>
    {
        public LexerBase<TToken, TTokenKind, TErrorCode> Lexer { get; }
        private readonly CommonTokenSet commonTokens;
        private readonly CommonErrorSet commonErrors;
        private readonly bool allowAssemblyStyleSingleLineComments; // allows single-line comments to be prefixed with ';'
        private int startPos, endPos;
        private int startLine, endLine;
        private int startColumn, endColumn;

        public TToken Current { get; private set; }

        object IEnumerator.Current => Current;

        protected SourceRange TokenLocation
            => new((startLine, startColumn, Lexer.FilePath), (endLine, endColumn - 1, Lexer.FilePath));
        protected ReadOnlyMemory<char> TokenLexeme
            => Lexer.Source.AsMemory(startPos, endPos - startPos);

        public EnumeratorBase(LexerBase<TToken, TTokenKind, TErrorCode> lexer, CommonTokenSet commonTokens, CommonErrorSet commonErrors, bool allowAssemblyStyleSingleLineComments)
        {
            this.commonTokens = commonTokens;
            this.commonErrors = commonErrors;
            this.allowAssemblyStyleSingleLineComments = allowAssemblyStyleSingleLineComments;
            Lexer = lexer;
            Reset();
        }

        public void Dispose() => GC.SuppressFinalize(this);

        public bool MoveNext()
        {
            if (TokenKindEquals(Current.Kind, commonTokens.EOF))
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

        protected void Error(TErrorCode code, string message, SourceRange location)
            => Lexer.Diagnostics.Add(code.AsInteger<TErrorCode, int>(), DiagnosticTag.Error, message, location);

        protected TToken BadToken(TErrorCode code, string message)
        {
            Error(code, message, TokenLocation);
            return NewToken(commonTokens.Bad);
        }

        protected TToken UnexpectedCharacterBadToken()
            => BadToken(commonErrors.UnexpectedCharacter, $"Unexpected character '{TokenLexeme}'");

        protected TToken IncompleteStringBadToken()
            => BadToken(commonErrors.IncompleteString, "Incomplete string");

        protected TToken InvalidIntegerLiteralBadToken()
            => BadToken(commonErrors.InvalidIntegerLiteral, "Invalid integer literal");

        protected TToken InvalidFloatLiteralBadToken()
            => BadToken(commonErrors.InvalidFloatLiteral, "Invalid float literal");

        private TToken NextToken()
        {
            SkipAllWhiteSpaces();
            Drop(); // start a new token

            var c = Next();
            if (!TryLexSpecificToken(c, out var token))
            {
                token = LexCommonToken(c);
            }
            return token;
        }

        private TToken LexCommonToken(char c)
        {
            return c switch
            {
                '.' => IsDecimalDigit(Peek()) ? LexFloatAfterDot() : NewToken(commonTokens.Dot),
                ',' => NewToken(commonTokens.Comma),
                '(' => NewToken(commonTokens.OpenParen),
                ')' => NewToken(commonTokens.CloseParen),
                ':' => NewToken(commonTokens.Colon),
                '#' => NewToken(commonTokens.Hash),
                '"' or '\'' => LexString(),
                '`' => LexHashString(),
                '\n' => LexEOS(),
                // NOTE: by default +/- are included in number tokens
                '-' or '+' when IsDecimalDigit(Peek()) => LexNumberWithSign(),
                >= '0' and <= '9' => LexNumber(),

                EOF => NewToken(commonTokens.EOF, SourceRange.EOF(Lexer.FilePath)),
                _ => UnexpectedCharacterBadToken()
            };
        }

        protected abstract bool TryLexSpecificToken(char c, out TToken token);

        // NOTE: Lex* methods assume that the first character of the token was already consumed (i.e. Next() was already called for the first char)

        /// <summary>
        /// Lexes single-quote or double-quote strings.
        /// </summary>
        private TToken LexString()
        {
            var quote = Peek(-1);
            Debug.Assert(quote is '"' or '\'');
            return LexStringLikeToken(commonTokens.String, quote);
        }

        /// <summary>
        /// Lexes hashed strings.
        /// </summary>
        private TToken LexHashString()
        {
            Debug.Assert(Peek(-1) is '`');
            return LexStringLikeToken(commonTokens.Integer, '`');
        }

        private TToken LexStringLikeToken(TTokenKind kind, char delimiter)
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
                        Error(commonErrors.UnrecognizedEscapeSequence, "Unrecognized escape sequence",
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

        private TToken LexNumberWithSign()
        {
            Debug.Assert(Peek(-1) is '-' or '+');
            Debug.Assert(Peek(0) is >= '0' and <= '9');

            Next();
            return LexNumber();
        }

        private TToken LexNumber()
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

        private TToken LexInteger()
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

            return NewToken(commonTokens.Integer);
        }

        private TToken? TryLexFloatExponent()
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
                return NewToken(commonTokens.Float);
            }
            else
            {
                // incomplete exponent, e.g. 1e, 1e+, 1e-
                return InvalidFloatLiteralBadToken();
            }
        }

        private TToken LexFloatAfterDot()
        {
            Debug.Assert(Peek(-1) is '.');
            Debug.Assert(IsDecimalDigit(Peek(0)));

            NextWhile(IsDecimalDigit);
            return TryLexFloatExponent() ?? NewToken(commonTokens.Float);
        }

        /// <summary>
        /// Merges multiple sequential empty lines in a single EOS token.
        /// </summary>
        private TToken LexEOS()
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
            return NewToken(commonTokens.EOS);
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
            if ((Peek(0) != '/' || Peek(1) != '/') && (!allowAssemblyStyleSingleLineComments || Peek(0) != ';'))
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
                    Error(commonErrors.OpenComment, "Reached end-of-file without closing the comment, expected '*/'", TokenLocation);
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

        protected abstract TToken CreateToken(TTokenKind kind, ReadOnlyMemory<char> lexeme, SourceRange location = default);
        protected TToken NewToken(TTokenKind kind, SourceRange? loc = null) => CreateToken(kind, TokenLexeme, loc ?? TokenLocation);

        protected void Drop()
        {
            startPos = endPos;
            startLine = endLine;
            startColumn = endColumn;
        }

        protected char Peek(int offset = 0)
        {
            var peekPos = endPos + offset;
            return peekPos >= 0 && peekPos < Lexer.Source.Length ? Lexer.Source[peekPos] : EOF;
        }

        protected char Next(bool dropTokenOnNewLine = true)
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

        protected bool NextIf(char expected)
        {
            if (Peek() == expected)
            {
                Next();
                return true;
            }

            return false;
        }

        protected bool NextIf(Predicate<char> isExpected)
        {
            if (isExpected(Peek()))
            {
                Next();
                return true;
            }

            return false;
        }

        protected void NextWhile(Predicate<char> isExpected)
        {
            while (NextIf(isExpected)) { /*empty*/ }
        }
    }
}

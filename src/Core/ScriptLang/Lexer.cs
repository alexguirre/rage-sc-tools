namespace ScTools.ScriptLang
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

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
            private ReadOnlyMemory<char> TokenContents
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

            private Token NextToken()
            {
                // advance
                SkipWhiteSpace();
                Drop();

                var token = Next() switch
                {
                    '.' => NewToken(TokenKind.Dot),
                    ',' => NewToken(TokenKind.Comma),
                    '(' => NewToken(TokenKind.OpenParen),
                    ')' => NewToken(TokenKind.CloseParen),
                    '[' => NewToken(TokenKind.OpenBracket),
                    ']' => NewToken(TokenKind.CloseBracket),
                    '=' => NextIf('=') ? NewToken(TokenKind.EqualsEquals) : NewToken(TokenKind.Equals),
                    '+' => NextIf('=') ? NewToken(TokenKind.PlusEquals) : NewToken(TokenKind.Plus),
                    '-' => NextIf('=') ? NewToken(TokenKind.MinusEquals) : NewToken(TokenKind.Minus),
                    '*' => NextIf('=') ? NewToken(TokenKind.AsteriskEquals) : NewToken(TokenKind.Asterisk),
                    '/' => NextIf('=') ? NewToken(TokenKind.SlashEquals) : NewToken(TokenKind.Slash),
                    '%' => NextIf('=') ? NewToken(TokenKind.PercentEquals) : NewToken(TokenKind.Percent),
                    '&' => NextIf('=') ? NewToken(TokenKind.AmpersandEquals) : NewToken(TokenKind.Ampersand),
                    '^' => NextIf('=') ? NewToken(TokenKind.CaretEquals) : NewToken(TokenKind.Caret),
                    '|' => NextIf('=') ? NewToken(TokenKind.BarEquals) : NewToken(TokenKind.Bar),
                    '<' => NextIf('=') ? NewToken(TokenKind.LessThanEquals) :      // <=
                           NextIf('<') ? NewToken(TokenKind.LessThanLessThan) :    // <<
                           NextIf('>') ? NewToken(TokenKind.LessThanGreaterThan) : // <>
                                         NewToken(TokenKind.LessThan),
                    '>' => NextIf('=') ? NewToken(TokenKind.GreaterThanEquals) :      // >=
                           NextIf('>') ? NewToken(TokenKind.GreaterThanGreaterThan) : // >>
                                         NewToken(TokenKind.GreaterThan),
                    ':' => NewToken(TokenKind.Colon),

                    '"' or '\'' => LexString(),
                    '`' => LexHashString(),

                    '\\' => NextIf('\n') ? NextToken() : // statement continuation: \ followed by new line
                                           NewToken(TokenKind.Bad), // TODO: add error message

                    '\n' => NewToken(TokenKind.EOS),

                    EOF => NewToken(TokenKind.EOF, SourceRange.EOF(Lexer.FilePath)),
                    char c when IsIdentifierStartChar(c) => LexIdentifierLikeToken(),
                    _ => NewToken(TokenKind.Bad)  // TODO: add error message
                };

                return token;
            }

            // NOTE: Lex* methods assume that the token first character was already consumed (i.e. Next() was already called for the first char)

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
                return LexStringLikeToken(TokenKind.HashString, '`');
            }

            private Token LexStringLikeToken(TokenKind kind, char delimiter)
            {
                while (true)
                {
                    var curr = Peek();
                    if (curr == EOF || curr == '\n')
                    {
                        Error(ErrorCode.LexerIncompleteString, "Incomplete string", TokenLocation);
                        return NewToken(TokenKind.Bad);
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

            /// <summary>
            /// Lexes tokens that look like identifiers (i.e. identifiers, keywords, bool literals and null literal)
            /// </summary>
            private Token LexIdentifierLikeToken()
            {
                // TODO: lex integers and floats
                var ident = LexIdentifier();
                var identStr = ident.Contents.ToString();
                if (IsKeyword(identStr) && Enum.TryParse<TokenKind>(identStr, ignoreCase: true, out var keywordKind))
                {
                    return NewToken(keywordKind);
                }

                if (IsNullLiteral(identStr))
                {
                    return NewToken(TokenKind.Null);
                }

                if (IsBooleanLiteral(identStr))
                {
                    return NewToken(TokenKind.Boolean);
                }

                return ident;
            }

            private Token LexIdentifier()
            {
                Debug.Assert(IsIdentifierStartChar(Peek(-1)));
                while (NextIf(IsIdentifierChar)) { /*empty*/ }

                return NewToken(TokenKind.Identifier);
            }

            private void SkipWhiteSpace()
            {
                while (NextIf(IsWhiteSpaceChar)) { /*empty*/ }
            }

            private Token NewToken(TokenKind kind, SourceRange? loc = null) => new() { Kind = kind, Contents = TokenContents, Location = loc ?? TokenLocation };

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

            private char Next()
            {
                if (endPos < Lexer.Source.Length)
                {
                    if (Peek(-1) == '\n')
                    {
                        // starting new line
                        endLine++;
                        endColumn = 1;
                        Drop(); // start a new token
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
        }






        ///// <summary>
        ///// Reads the next token.
        ///// </summary>
        ///// <param name="token">The next token.</param>
        ///// <returns><c>true</c> if there are tokens remaining; otherwise, <c>false</c>.</returns>
        //private bool ReadToken(out Token token)
        //{
        //    startPos = endPos;
        //    startLine = endLine;
        //    startColumn = endColumn;

        //    switch (Peek())
        //    {
        //        case EOF:
        //            token = NewToken(TokenKind.EOF);
        //            return false;

        //        case >= '0' and <= '9':
        //            return ReadNumber(out token);

        //        case '\'' or '"':
        //            return ReadString(out token);

        //        case '\n':
        //            return ReadNewLine(out token);

        //        case '/':
        //            return ReadForwardSlash(out token);

        //        case ' ' or '\t' or '\r':
        //            return ReadWhitespace(out token);

        //        case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_':
        //            return ReadIdentifierOrKeyword(out token);

        //        default:
        //            return ReadSymbol(out token);
        //    }
        //}

        //private bool ReadNumber(out Token token)
        //{
        //    while (Peek() is >= '0' and <= '9')
        //           Next();
        //    token = NewToken(TokenKind.Integer);
        //    return true;
        //}

        //private bool ReadString(out Token token)
        //{
        //    var quote = Next();
        //    while (true) // TODO: consider escape sequences
        //    {
        //        // TODO: TESTTEST
        //        var c = Peek();
        //        if (c == EOF || c == '\n')
        //        {
        //            Error(ErrorCode.LexerIncompleteString, TokenLocation);
        //            break;
        //        }

        //        Next();
        //        if (c == quote)
        //        {
        //            break;
        //        }
        //    }

        //    token = NewToken(TokenKind.String);
        //    return true;
        //}

        //private bool ReadIdentifierOrKeyword(out Token token)
        //{
        //    while (Peek() is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_')
        //        Next();

        //    token = NewToken(IsKeyword(TokenContents) ? TokenKind.Keyword : TokenKind.Identifier);
        //    return true;
        //}

        //private bool ReadNewLine(out Token token)
        //{
        //    Next(); // skip newline
        //    token = NewToken(TokenKind.NewLine);
        //    endLine++;
        //    endColumn = 1;
        //    return true;
        //}

        //private bool ReadWhitespace(out Token token)
        //{
        //    Next(); // skip whitespace
        //    return ReadToken(out token);
        //}

        //private bool ReadForwardSlash(out Token token)
        //{
        //    if (Peek(1) == '/') // comment
        //    {
        //        SkipComment();
        //    }
        //    else if (Peek(1) == '*') // multi-line comment
        //    {
        //        SkipMultiLineComment();
        //    }
        //    else // division operator
        //    {
        //        return ReadSymbol(out token);
        //    }
        //}

        //private bool ReadSymbol(out Token token)
        //{
        //    Next(); // skip symbol
        //    token = NewToken(TokenKind.Symbol);
        //    return true;
        //}

        //private void SkipComment()
        //{
        //    Next(); Next(); // skip "//"
        //    while (Peek() != EOF && Peek() != '\n')
        //    {
        //        // skip comment text until newline
        //        Next();
        //    }
        //}

        //private void SkipMultiLineComment()
        //{
        //    Next(); Next(); // skip "/*"
        //    while (Peek() != EOF && Peek(0) != '*' && Peek(1) != '/')
        //    {
        //        // skip comment text until "*/"
        //        Next();
        //    }
        //}

        private static bool IsKeyword(string str)
            => Keywords.Contains(str);
        private static bool IsNullLiteral(string str)
            => Parser.CaseInsensitiveComparer.Equals(str, "NULL");
        private static bool IsBooleanLiteral(string str)
            => Parser.CaseInsensitiveComparer.Equals(str, "TRUE") ||
               Parser.CaseInsensitiveComparer.Equals(str, "FALSE");


        private const char EOF = char.MaxValue;

        private static string NormalizeSource(string source)
            => source.ReplaceLineEndings("\n");

        private static bool IsWhiteSpaceChar(char c)
            => c is not ('\n' or EOF) && char.IsWhiteSpace(c);
        private static bool IsIdentifierStartChar(char c)
            => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_';
        private static bool IsIdentifierChar(char c)
            => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';

        private static readonly HashSet<string> Keywords =
            new(Enum.GetValues<TokenKind>().Where(t => t.IsKeyword()).Select(t => t.ToString()),
                Parser.CaseInsensitiveComparer);
    }
}

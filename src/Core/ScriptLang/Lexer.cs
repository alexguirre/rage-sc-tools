namespace ScTools.ScriptLang;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public sealed class Lexer : LexerBase<Token, TokenKind, ErrorCode>
{
    public Lexer(string filePath, string source, DiagnosticsReport diagnostics)
        : base(filePath, source, diagnostics)
    {
    }

    public override Enumerator GetEnumerator() => new(this);

    private static bool IsKeyword(string str)
        => Keywords.Contains(str);
    private static bool IsNullLiteral(string str)
        => Parser.CaseInsensitiveComparer.Equals(str, TokenLexemes.Null);
    private static bool IsBooleanLiteral(string str)
        => Parser.CaseInsensitiveComparer.Equals(str, TokenLexemes.BooleanTrue) ||
           Parser.CaseInsensitiveComparer.Equals(str, TokenLexemes.BooleanFalse);

    private static bool IsIdentifierStartChar(char c)
        => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_';
    private static bool IsIdentifierChar(char c)
        => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9');

    internal static readonly HashSet<string> Keywords =
        new(Enum.GetValues<TokenKind>().Where(t => t.IsKeyword()).Select(t => t.ToString()),
            Parser.CaseInsensitiveComparer);

    public sealed class Enumerator : EnumeratorBase
    {
        public Enumerator(Lexer lexer)
            : base(lexer,
                new(Bad: TokenKind.Bad,
                    Dot: TokenKind.Dot,
                    Comma: TokenKind.Comma,
                    OpenParen: TokenKind.OpenParen,
                    CloseParen: TokenKind.CloseParen,
                    Colon: TokenKind.Colon,
                    Integer: TokenKind.Integer,
                    Float: TokenKind.Float,
                    String: TokenKind.String,
                    EOS: TokenKind.EOS,
                    EOF: TokenKind.EOF),
                new(UnexpectedCharacter: ErrorCode.LexerUnexpectedCharacter,
                    IncompleteString: ErrorCode.LexerIncompleteString,
                    UnrecognizedEscapeSequence: ErrorCode.LexerUnrecognizedEscapeSequence,
                    OpenComment: ErrorCode.LexerOpenComment,
                    InvalidIntegerLiteral: ErrorCode.LexerInvalidIntegerLiteral,
                    InvalidFloatLiteral: ErrorCode.LexerInvalidFloatLiteral))
        {
        }

        protected override Token CreateToken(TokenKind kind, ReadOnlyMemory<char> lexeme, SourceRange location = default)
            => new(kind, lexeme, location);

        protected override bool TryLexSpecificToken(char c, out Token token)
        {
            Token? possibleToken = c switch
            {
                '[' => NewToken(TokenKind.OpenBracket),
                ']' => NewToken(TokenKind.CloseBracket),
                '=' => NextIf('=') ? NewToken(TokenKind.EqualsEquals) : NewToken(TokenKind.Equals),
                // NOTE: number tokens do not include +/- in the lexeme as here we don't know if the sign is used as a unary
                //       operator or as a binary operator. The parser will handle this.
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
                _ when IsIdentifierStartChar(c) => LexIdentifierLikeToken(),

                _ => null
            };

            token = possibleToken ?? default;
            return possibleToken.HasValue;
        }

        /// <summary>
        /// Lexes tokens that look like identifiers (i.e. identifiers, keywords, bool literals and null literal)
        /// </summary>
        private Token LexIdentifierLikeToken()
        {
            var ident = LexIdentifier();
            var identStr = ident.Lexeme.ToString();
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
            NextWhile(IsIdentifierChar);

            return NewToken(TokenKind.Identifier);
        }
    }
}

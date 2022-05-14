namespace ScTools.ScriptAssembly;

using System;
using System.Diagnostics;

public sealed class Lexer : LexerBase<Token, TokenKind, ErrorCode>
{
    public Lexer(string filePath, string source, DiagnosticsReport diagnostics)
        : base(filePath, source, diagnostics)
    {
    }

    public override Enumerator GetEnumerator() => new(this);

    private static bool IsIdentifierStartChar(char c)
    => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_';
    private static bool IsIdentifierChar(char c)
        => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9');

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
                    InvalidFloatLiteral: ErrorCode.LexerInvalidFloatLiteral),
                allowAssemblyStyleSingleLineComments: true)
        {
        }

        protected override Token CreateToken(TokenKind kind, ReadOnlyMemory<char> lexeme, SourceRange location = default)
            => new(kind, lexeme, location);

        protected override bool TryLexSpecificToken(char c, out Token token)
        {
            if (IsIdentifierStartChar(c))
            {
                token = LexIdentifier();
                return true;
            }

            token = default;
            return false;
        }

        private Token LexIdentifier()
        {
            Debug.Assert(IsIdentifierStartChar(Peek(-1)));
            NextWhile(IsIdentifierChar);

            return NewToken(TokenKind.Identifier);
        }
    }
}

namespace ScTools.ScriptAssembly;

using System;
using System.Diagnostics;

public enum TokenKind
{
    /// <summary>
    /// Illegal sequence of characters.
    /// </summary>
    Bad = 0,

    Identifier, // [a-zA-Z_][a-zA-Z_0-9]*

    // Symbols
    Dot,                    // .
    Comma,                  // ,
    OpenParen,              // (
    CloseParen,             // )
    Colon,                  // :
    Hash,                   // #

    // Literals
    String,     // "...", '...'
    Integer,    // decimal or hexadecimal prefixed by 0x, or hash strings `...`
    Float,

    /// <summary>
    /// End-of-statement. A new line (\n) except if it is escaped with \.
    /// In that case the new line is considered a whitespace and the statement continues.
    /// </summary>
    EOS,
    /// <summary>
    /// End-of-file.
    /// </summary>
    EOF,
}

public static class TokenLexemes
{
    public const string Dot = ".";
    public const string Comma = ",";
    public const string OpenParen = "(";
    public const string CloseParen = ")";
    public const string Colon = ":";
    public const string Hash = "#";

    public const string EOS = "\n";
    public const string EOF = "";
}

public static class TokenKindExtensions
{
    public static string GetCanonicalLexeme(this TokenKind kind)
        => kind switch
        {
            TokenKind.Dot => TokenLexemes.Dot,
            TokenKind.Comma => TokenLexemes.Comma,
            TokenKind.OpenParen => TokenLexemes.OpenParen,
            TokenKind.CloseParen => TokenLexemes.CloseParen,
            TokenKind.Colon => TokenLexemes.Colon,
            TokenKind.Hash => TokenLexemes.Hash,
            TokenKind.EOS => TokenLexemes.EOS,
            TokenKind.EOF => TokenLexemes.EOF,
            _ => throw new ArgumentException($"Token '{kind}' has no canonical lexeme", nameof(kind)),
        };

    public static bool HasCanonicalLexeme(this TokenKind kind)
        => kind is not (TokenKind.Bad or TokenKind.Identifier or TokenKind.String or
                       TokenKind.Integer or TokenKind.Float);

    public static Token Create(this TokenKind kind, string lexeme, SourceRange location = default)
        => new(kind, lexeme, location);
    public static Token Create(this TokenKind kind, SourceRange location = default)
        => new(kind, kind.GetCanonicalLexeme(), location);
}

public readonly record struct Token : IToken<Token, TokenKind>
{
    public TokenKind Kind { get; init; }
    public bool IsMissing { get; init; }
    public ReadOnlyMemory<char> Lexeme { get; init; }
    public SourceRange Location { get; init; }

    public Token(TokenKind kind, ReadOnlyMemory<char> lexeme, SourceRange location = default)
    {
        Kind = kind;
        IsMissing = false;
        Lexeme = lexeme;
        Location = location;
    }

    public Token(TokenKind kind, string lexeme, SourceRange location = default)
        : this(kind, lexeme.AsMemory(), location)
    { }

    public override string ToString()
        => $"{{ {nameof(Kind)}: {Kind}, {nameof(Lexeme)}: {Lexeme}, {nameof(Location)}: {Location} }}";

    public string GetStringLiteral()
    {
        Debug.Assert(Kind is TokenKind.String);
        return Lexeme[1..^1].Unescape();
    }

    public int GetIntLiteral()
    {
        Debug.Assert(Kind is TokenKind.Integer);
        if (Lexeme.Length >= 2)
        {
            var lexemeSpan = Lexeme.Span;
            if (lexemeSpan[0] == '`' && lexemeSpan[^1] == '`')
            {
                return unchecked((int)lexemeSpan[1..^1].Unescape().ToLowercaseHash());
            }
        }

        return Lexeme.ParseAsInt();
    }

    public long GetInt64Literal()
    {
        Debug.Assert(Kind is TokenKind.Integer);
        if (Lexeme.Length >= 2)
        {
            var lexemeSpan = Lexeme.Span;
            if (lexemeSpan[0] == '`' && lexemeSpan[^1] == '`')
            {
                return lexemeSpan[1..^1].Unescape().ToLowercaseHash();
            }
        }

        return Lexeme.ParseAsInt64();
    }

    public float GetFloatLiteral()
    {
        Debug.Assert(Kind is TokenKind.Integer or TokenKind.Float);
        return Lexeme.ParseAsFloat();
    }

    public static Token Identifier(string name, SourceRange location = default) => TokenKind.Identifier.Create(name, location);
    public static Token Integer(int value, SourceRange location = default) => TokenKind.Integer.Create(value.ToString(), location);
    public static Token Float(float value, SourceRange location = default) => TokenKind.Float.Create(value.ToString("G9"), location);
    public static Token String(string value, SourceRange location = default) => TokenKind.String.Create($"'{value.Escape()}'", location);

    public static Token Create(TokenKind kind, ReadOnlyMemory<char> lexeme, SourceRange location = default)
        => new(kind, lexeme, location);
}

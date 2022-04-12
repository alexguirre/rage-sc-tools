namespace ScTools.ScriptLang;

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
    OpenBracket,            // [
    CloseBracket,           // ]
    Equals,                 // =
    Plus,                   // +
    Minus,                  // -
    Asterisk,               // *
    Slash,                  // /
    Percent,                // %
    Ampersand,              // &
    Caret,                  // ^
    Bar,                    // |
    PlusEquals,             // +=
    MinusEquals,            // -=
    AsteriskEquals,         // *=
    SlashEquals,            // /=
    PercentEquals,          // %=
    AmpersandEquals,        // &=
    CaretEquals,            // ^=
    BarEquals,              // |=
    LessThan,               // <
    GreaterThan,            // >
    LessThanEquals,         // <=
    GreaterThanEquals,      // >=
    EqualsEquals,           // ==
    LessThanGreaterThan,    // <>
    LessThanLessThan,       // <<
    GreaterThanGreaterThan, // >>
    Colon,                  // :

    // Literals
    String,     // "...", '...'
    HashString, // `...`
    Integer,    // decimal or hexadecimal prefixed by 0x
    Float,
    Boolean,    // TRUE, FALSE
    Null,       // NULL

    // Keywords
    SCRIPT,
    ENDSCRIPT,
    PROC,
    ENDPROC,
    FUNC,
    ENDFUNC,
    STRUCT,
    ENDSTRUCT,
    ENUM,
    ENDENUM,
    PROTO,
    NATIVE,
    NOT,
    AND,
    OR,
    IF,
    ELIF,
    ELSE,
    ENDIF,
    WHILE,
    ENDWHILE,
    REPEAT,
    ENDREPEAT,
    SWITCH,
    ENDSWITCH,
    CASE,
    DEFAULT,
    BREAK,
    CONTINUE,
    RETURN,
    GOTO,
    SCRIPT_HASH,
    USING,
    CONST,
    GLOBAL,
    ENDGLOBAL,
    SIZE_OF,

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

public static class TokenKindExtensions
{
    public static bool IsKeyword(this TokenKind kind)
        => kind >= TokenKind.SCRIPT && kind <= TokenKind.SIZE_OF;
}

public readonly record struct Token
{
    public TokenKind Kind { get; init; }
    public ReadOnlyMemory<char> Lexeme { get; init; }
    public SourceRange Location { get; init; }

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
        return Lexeme.ParseAsInt();
    }

    public float GetFloatLiteral()
    {
        Debug.Assert(Kind is TokenKind.Integer or TokenKind.Float);
        return Lexeme.ParseAsFloat();
    }

    public bool GetBoolLiteral()
    {
        Debug.Assert(Kind is TokenKind.Boolean);
        return Lexeme.ParseAsBool();
    }


    public static Token Identifier(string name, SourceRange location = default) => new() { Kind = TokenKind.Identifier, Lexeme = name.AsMemory(), Location = location };
    public static Token Integer(int value, SourceRange location = default) => new() { Kind = TokenKind.Integer, Lexeme = value.ToString().AsMemory(), Location = location };
    public static Token Float(float value, SourceRange location = default) => new() { Kind = TokenKind.Float, Lexeme = value.ToString("G9").AsMemory(), Location = location };
    public static Token Bool(bool value, SourceRange location = default) => new() { Kind = TokenKind.Boolean, Lexeme = (value ? "TRUE" : "FALSE").AsMemory(), Location = location };
    public static Token String(string value, SourceRange location = default) => new() { Kind = TokenKind.String, Lexeme = $"'{value.Escape()}'".AsMemory(), Location = location };
    public static Token Null(SourceRange location = default) => new() { Kind = TokenKind.Null, Lexeme = "NULL".AsMemory(), Location = location };
    public static Token Plus(SourceRange location = default) => new() { Kind = TokenKind.Plus, Lexeme = "+".AsMemory(), Location = location };
}

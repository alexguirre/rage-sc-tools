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
    Hash,                   // #

    // Literals
    String,     // "...", '...'
    Integer,    // decimal or hexadecimal prefixed by 0x, or hash strings `...`
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
    DEBUGONLY,
    STRUCT,
    ENDSTRUCT,
    ENUM,
    HASH_ENUM,
    STRICT_ENUM,
    ENDENUM,
    TYPEDEF,
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
    CONST_INT,
    CONST_FLOAT,
    GLOBAL,
    ENDGLOBAL,

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
    public const string OpenBracket = "[";
    public const string CloseBracket = "]";
    public new const string Equals = "=";
    public const string Plus = "+";
    public const string Minus = "-";
    public const string Asterisk = "*";
    public const string Slash = "/";
    public const string Percent = "%";
    public const string Ampersand = "&";
    public const string Caret = "^";
    public const string Bar = "|";
    public const string PlusEquals = "+=";
    public const string MinusEquals = "-=";
    public const string AsteriskEquals = "*=";
    public const string SlashEquals = "/=";
    public const string PercentEquals = "%=";
    public const string AmpersandEquals = "&=";
    public const string CaretEquals = "^=";
    public const string BarEquals = "|=";
    public const string LessThan = "<";
    public const string GreaterThan = ">";
    public const string LessThanEquals = "<=";
    public const string GreaterThanEquals = ">=";
    public const string EqualsEquals = "==";
    public const string LessThanGreaterThan = "<>";
    public const string LessThanLessThan = "<<";
    public const string GreaterThanGreaterThan = ">>";
    public const string Colon = ":";
    public const string Hash = "#";

    public const string BooleanTrue = "TRUE";
    public const string BooleanFalse = "FALSE";
    public const string Null = "NULL";

    public const string SCRIPT = nameof(TokenKind.SCRIPT);
    public const string ENDSCRIPT = nameof(TokenKind.ENDSCRIPT);
    public const string PROC = nameof(TokenKind.PROC);
    public const string ENDPROC = nameof(TokenKind.ENDPROC);
    public const string FUNC = nameof(TokenKind.FUNC);
    public const string ENDFUNC = nameof(TokenKind.ENDFUNC);
    public const string DEBUGONLY = nameof(TokenKind.DEBUGONLY);
    public const string STRUCT = nameof(TokenKind.STRUCT);
    public const string ENDSTRUCT = nameof(TokenKind.ENDSTRUCT);
    public const string ENUM = nameof(TokenKind.ENUM);
    public const string HASH_ENUM = nameof(TokenKind.HASH_ENUM);
    public const string STRICT_ENUM = nameof(TokenKind.STRICT_ENUM);
    public const string ENDENUM = nameof(TokenKind.ENDENUM);
    public const string TYPEDEF = nameof(TokenKind.TYPEDEF);
    public const string NATIVE = nameof(TokenKind.NATIVE);
    public const string NOT = nameof(TokenKind.NOT);
    public const string AND = nameof(TokenKind.AND);
    public const string OR = nameof(TokenKind.OR);
    public const string IF = nameof(TokenKind.IF);
    public const string ELIF = nameof(TokenKind.ELIF);
    public const string ELSE = nameof(TokenKind.ELSE);
    public const string ENDIF = nameof(TokenKind.ENDIF);
    public const string WHILE = nameof(TokenKind.WHILE);
    public const string ENDWHILE = nameof(TokenKind.ENDWHILE);
    public const string REPEAT = nameof(TokenKind.REPEAT);
    public const string ENDREPEAT = nameof(TokenKind.ENDREPEAT);
    public const string SWITCH = nameof(TokenKind.SWITCH);
    public const string ENDSWITCH = nameof(TokenKind.ENDSWITCH);
    public const string CASE = nameof(TokenKind.CASE);
    public const string DEFAULT = nameof(TokenKind.DEFAULT);
    public const string BREAK = nameof(TokenKind.BREAK);
    public const string CONTINUE = nameof(TokenKind.CONTINUE);
    public const string RETURN = nameof(TokenKind.RETURN);
    public const string GOTO = nameof(TokenKind.GOTO);
    public const string SCRIPT_HASH = nameof(TokenKind.SCRIPT_HASH);
    public const string USING = nameof(TokenKind.USING);
    public const string CONST_INT = nameof(TokenKind.CONST_INT);
    public const string CONST_FLOAT = nameof(TokenKind.CONST_FLOAT);
    public const string GLOBAL = nameof(TokenKind.GLOBAL);
    public const string ENDGLOBAL = nameof(TokenKind.ENDGLOBAL);

    public const string EOS = "\n";
    public const string EOF = "";
}

public static class TokenKindExtensions
{
    public static bool IsKeyword(this TokenKind kind)
        => kind >= TokenKind.SCRIPT && kind <= TokenKind.ENDGLOBAL;

    public static string GetCanonicalLexeme(this TokenKind kind)
        => kind switch
        {
            TokenKind.Dot => TokenLexemes.Dot,
            TokenKind.Comma => TokenLexemes.Comma,
            TokenKind.OpenParen => TokenLexemes.OpenParen,
            TokenKind.CloseParen => TokenLexemes.CloseParen,
            TokenKind.OpenBracket => TokenLexemes.OpenBracket,
            TokenKind.CloseBracket => TokenLexemes.CloseBracket,
            TokenKind.Equals => TokenLexemes.Equals,
            TokenKind.Plus => TokenLexemes.Plus,
            TokenKind.Minus => TokenLexemes.Minus,
            TokenKind.Asterisk => TokenLexemes.Asterisk,
            TokenKind.Slash => TokenLexemes.Slash,
            TokenKind.Percent => TokenLexemes.Percent,
            TokenKind.Ampersand => TokenLexemes.Ampersand,
            TokenKind.Caret => TokenLexemes.Caret,
            TokenKind.Bar => TokenLexemes.Bar,
            TokenKind.PlusEquals => TokenLexemes.PlusEquals,
            TokenKind.MinusEquals => TokenLexemes.MinusEquals,
            TokenKind.AsteriskEquals => TokenLexemes.AsteriskEquals,
            TokenKind.SlashEquals => TokenLexemes.SlashEquals,
            TokenKind.PercentEquals => TokenLexemes.PercentEquals,
            TokenKind.AmpersandEquals => TokenLexemes.AmpersandEquals,
            TokenKind.CaretEquals => TokenLexemes.CaretEquals,
            TokenKind.BarEquals => TokenLexemes.BarEquals,
            TokenKind.LessThan => TokenLexemes.LessThan,
            TokenKind.GreaterThan => TokenLexemes.GreaterThan,
            TokenKind.LessThanEquals => TokenLexemes.LessThanEquals,
            TokenKind.GreaterThanEquals => TokenLexemes.GreaterThanEquals,
            TokenKind.EqualsEquals => TokenLexemes.EqualsEquals,
            TokenKind.LessThanGreaterThan => TokenLexemes.LessThanGreaterThan,
            TokenKind.LessThanLessThan => TokenLexemes.LessThanLessThan,
            TokenKind.GreaterThanGreaterThan => TokenLexemes.GreaterThanGreaterThan,
            TokenKind.Colon => TokenLexemes.Colon,
            TokenKind.Hash => TokenLexemes.Hash,
            TokenKind.Null => TokenLexemes.Null,
            TokenKind.SCRIPT => TokenLexemes.SCRIPT,
            TokenKind.ENDSCRIPT => TokenLexemes.ENDSCRIPT,
            TokenKind.PROC => TokenLexemes.PROC,
            TokenKind.ENDPROC => TokenLexemes.ENDPROC,
            TokenKind.FUNC => TokenLexemes.FUNC,
            TokenKind.ENDFUNC => TokenLexemes.ENDFUNC,
            TokenKind.DEBUGONLY => TokenLexemes.DEBUGONLY,
            TokenKind.STRUCT => TokenLexemes.STRUCT,
            TokenKind.ENDSTRUCT => TokenLexemes.ENDSTRUCT,
            TokenKind.ENUM => TokenLexemes.ENUM,
            TokenKind.HASH_ENUM => TokenLexemes.HASH_ENUM,
            TokenKind.STRICT_ENUM => TokenLexemes.STRICT_ENUM,
            TokenKind.ENDENUM => TokenLexemes.ENDENUM,
            TokenKind.TYPEDEF => TokenLexemes.TYPEDEF,
            TokenKind.NATIVE => TokenLexemes.NATIVE,
            TokenKind.NOT => TokenLexemes.NOT,
            TokenKind.AND => TokenLexemes.AND,
            TokenKind.OR => TokenLexemes.OR,
            TokenKind.IF => TokenLexemes.IF,
            TokenKind.ELIF => TokenLexemes.ELIF,
            TokenKind.ELSE => TokenLexemes.ELSE,
            TokenKind.ENDIF => TokenLexemes.ENDIF,
            TokenKind.WHILE => TokenLexemes.WHILE,
            TokenKind.ENDWHILE => TokenLexemes.ENDWHILE,
            TokenKind.REPEAT => TokenLexemes.REPEAT,
            TokenKind.ENDREPEAT => TokenLexemes.ENDREPEAT,
            TokenKind.SWITCH => TokenLexemes.SWITCH,
            TokenKind.ENDSWITCH => TokenLexemes.ENDSWITCH,
            TokenKind.CASE => TokenLexemes.CASE,
            TokenKind.DEFAULT => TokenLexemes.DEFAULT,
            TokenKind.BREAK => TokenLexemes.BREAK,
            TokenKind.CONTINUE => TokenLexemes.CONTINUE,
            TokenKind.RETURN => TokenLexemes.RETURN,
            TokenKind.GOTO => TokenLexemes.GOTO,
            TokenKind.SCRIPT_HASH => TokenLexemes.SCRIPT_HASH,
            TokenKind.USING => TokenLexemes.USING,
            TokenKind.CONST_INT => TokenLexemes.CONST_INT,
            TokenKind.CONST_FLOAT => TokenLexemes.CONST_FLOAT,
            TokenKind.GLOBAL => TokenLexemes.GLOBAL,
            TokenKind.ENDGLOBAL => TokenLexemes.ENDGLOBAL,
            TokenKind.EOS => TokenLexemes.EOS,
            TokenKind.EOF => TokenLexemes.EOF,
            _ => throw new ArgumentException($"Token '{kind}' has no canonical lexeme", nameof(kind)),
        };

    public static bool HasCanonicalLexeme(this TokenKind kind)
        => kind is not (TokenKind.Bad or TokenKind.Identifier or TokenKind.String or
                       TokenKind.Integer or TokenKind.Float or TokenKind.Boolean);

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

    public static Token Identifier(string name, SourceRange location = default) => TokenKind.Identifier.Create(name, location);
    public static Token Integer(int value, SourceRange location = default) => TokenKind.Integer.Create(value.ToString(), location);
    public static Token Float(float value, SourceRange location = default) => TokenKind.Float.Create(value.ToString("G9"), location);
    public static Token Bool(bool value, SourceRange location = default) => TokenKind.Boolean.Create(value ? "TRUE" : "FALSE", location);
    public static Token String(string value, SourceRange location = default) => TokenKind.String.Create($"'{value.Escape()}'", location);

    public static Token Create(TokenKind kind, ReadOnlyMemory<char> lexeme, SourceRange location = default)
        => new(kind, lexeme, location);
}

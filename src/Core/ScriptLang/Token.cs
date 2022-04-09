using System;

namespace ScTools.ScriptLang
{
    public enum TokenKind
    {
        /// <summary>
        /// Illegal sequence of characters.
        /// </summary>
        Bad = 0,

        Identifier,

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
        Integer,
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

    public readonly struct Token
    {
        public TokenKind Kind { get; init; }
        public ReadOnlyMemory<char> Contents { get; init; }
        public SourceRange Location { get; init; }

        public override string ToString()
            => $"{{ {nameof(Kind)}: {Kind}, {nameof(Contents)}: {Contents}, {nameof(Location)}: {Location} }}";
    }
}

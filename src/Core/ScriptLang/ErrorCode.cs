namespace ScTools.ScriptLang
{
    public enum ErrorCode
    {
        // Lexer errors    0x0000 - 0x0FFF
        LexerUnexpectedCharacter = 0x0000,
        LexerIncompleteString = 0x0001,
        LexerUnrecognizedEscapeSequence = 0x0002,
        LexerOpenComment = 0x0003,
        LexerInvalidIntegerLiteral = 0x0004,
        LexerInvalidFloatLiteral = 0x0005,

        // Parser errors   0x1000 - 0x1FFF
        ParserUnexpectedToken = 0x1000,
        ParserUnknownDeclaration = 0x1001,
        ParserUnknownStatement = 0x1002,
        ParserUnknownExpression = 0x1003,
        ParserUnknownDeclarator = 0x1004,
        ParserExpressionAsStatement = 0x1005,
        ParserUsingAfterDeclaration = 0x1006,

        // Semantic errors 0x2000 - 0x2FFF
    }
}

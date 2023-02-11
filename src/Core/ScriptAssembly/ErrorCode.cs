namespace ScTools.ScriptAssembly;

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

    // Semantic errors 0x2000 - 0x2FFF
    SemanticSymbolAlreadyDefined = 0x2000,
    SemanticUndefinedSymbol = 0x2001,
    SemanticLabelAlreadyDefined = 0x2002,
    SemanticUndefinedLabel = 0x2003,

    // Preprocessor errors   0x3000 - 0x3FFF
    PreprocessorUnexpectedDirective = 0x3000,
    PreprocessorUnknownDirective = 0x3001,
    PreprocessorUnexpectedToken = 0x3002,
    PreprocessorOpenIfDirective = 0x3003,
}

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
        ParserVarInitializerNotAllowed = 0x1007,

        // Semantic errors 0x2000 - 0x2FFF
        SemanticSymbolAlreadyDefined = 0x2000,
        SemanticUndefinedSymbol = 0x2001,
        SemanticExpectedTypeSymbol = 0x2002,
        SemanticLabelAlreadyDefined = 0x2003,
        SemanticUndefinedLabel = 0x2004,
        SemanticExpectedLabel = 0x2005,
        SemanticConstantWithoutInitializer = 0x2006,
        SemanticInitializerExpressionIsNotConstant = 0x2007,
        SemanticTypeNotAllowedInConstant = 0x2008,
        SemanticBadUnaryOp = 0x2009,
        SemanticBadBinaryOp = 0x200A,
        SemanticUnknownField = 0x200B,
        SemanticScriptNameNotAllowedInExpression = 0x200C,
        SemanticCannotConvertType = 0x200D,
    }
}

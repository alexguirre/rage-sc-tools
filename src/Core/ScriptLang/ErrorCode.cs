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
        ParserReferenceNotAllowed = 0x1008,

        // Semantic errors 0x2000 - 0x2FFF
        SemanticSymbolAlreadyDefined = 0x2000,
        SemanticUndefinedSymbol = 0x2001,
        SemanticExpectedTypeSymbol = 0x2002,
        SemanticLabelAlreadyDefined = 0x2003,
        SemanticUndefinedLabel = 0x2004,
        SemanticConstantWithoutInitializer = 0x2005,
        SemanticInitializerExpressionIsNotConstant = 0x2006,
        SemanticTypeNotAllowedInConstant = 0x2007,
        SemanticBadUnaryOp = 0x2008,
        SemanticBadBinaryOp = 0x2009,
        SemanticUnknownField = 0x200A,
        SemanticScriptNameNotAllowedInExpression = 0x200B,
        SemanticCannotConvertType = 0x200C,
        SemanticExpectedValueInReturn = 0x200D,
        SemanticValueReturnedFromProcedure = 0x200E,
        SemanticTypeNotCallable = 0x200F,
        SemanticMismatchedArgumentCount = 0x2010,
        SemanticArgCannotPassType = 0x2011,
        SemanticArgCannotPassRefType = 0x2012,
        SemanticArgCannotPassNonLValueToRefParam = 0x2013,
        SemanticArgNotAnEnum = 0x2014,
        SemanticArgNotAnEnumType = 0x2015,
        SemanticArgNotAnArray = 0x2016,
    }
}

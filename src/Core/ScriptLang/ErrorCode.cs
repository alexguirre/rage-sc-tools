namespace ScTools.ScriptLang
{
    public enum ErrorCode
    {
        // Lexer errors    0x0000 - 0x0FFF
        LexerIncompleteString = 0x0000,
        LexerUnrecognizedEscapeSequence = 0x0001,

        // Parser errors   0x1000 - 0x1FFF

        // Semantic errors 0x2000 - 0x2FFF
    }
}

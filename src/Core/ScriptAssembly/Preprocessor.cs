namespace ScTools.ScriptAssembly;

public sealed class Preprocessor : PreprocessorBase<Token, TokenKind, ErrorCode>
{
    public Preprocessor(DiagnosticsReport diagnostics)
        : base(diagnostics,
            new(Hash: TokenKind.Hash,
                EOS: TokenKind.EOS,
                EOF: TokenKind.EOF),
            new(DirectiveWrongPlacement: ErrorCode.PreprocessorDirectiveWrongPlacement,
                UnknownDirective: ErrorCode.PreprocessorUnknownDirective,
                UnexpectedToken: ErrorCode.PreprocessorUnexpectedToken))
    {
    }
}

namespace ScTools.Tests;

using System;
using System.Linq;

using Xunit;

using static Xunit.Assert;

public abstract class PreprocessorTests<TToken, TTokenKind, TErrorCode>
    where TToken : struct, IToken<TToken, TTokenKind>
    where TTokenKind : struct, Enum
    where TErrorCode : struct, Enum
{
    protected abstract TTokenKind TokenKindIdentifier { get; }
    protected abstract TTokenKind TokenKindEOS { get; }
    protected abstract TTokenKind TokenKindEOF { get; }
    protected abstract ILexer<TToken, TTokenKind, TErrorCode> CreateLexer(string filePath, string source, DiagnosticsReport diagnostics);
    protected abstract IPreprocessor<TToken, TTokenKind, TErrorCode> CreatePreprocessor(DiagnosticsReport diagnostics);

    [Fact]
    public void IfEvaluatesToTrue()
    {
        var (tokens, diag) = Preprocess(
            @"
            aaa
            #if FOO
            bbb
            #endif
            ccc",
            new[] { "FOO" });

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(7, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (2, 13), (2, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenEqual(TokenKindIdentifier, "bbb", (4, 13), (4, 15), tokens[3]);
        TokenIsEOS(tokens[4]);
        TokenEqual(TokenKindIdentifier, "ccc", (6, 13), (6, 15), tokens[5]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void IfEvaluatesToFalse()
    {
        var (tokens, diag) = Preprocess(
            @"
            aaa
            #if FOO
            bbb
            #endif
            ccc");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(5, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (2, 13), (2, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenEqual(TokenKindIdentifier, "ccc", (6, 13), (6, 15), tokens[3]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void IfNotEvaluatesToFalse()
    {
        var (tokens, diag) = Preprocess(
            @"
            aaa
            #if NOT FOO
            bbb
            #endif
            ccc",
            new[] { "FOO" });

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(5, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (2, 13), (2, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenEqual(TokenKindIdentifier, "ccc", (6, 13), (6, 15), tokens[3]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void IfNotEvaluatesToTrue()
    {
        var (tokens, diag) = Preprocess(
            @"
            aaa
            #if NOT FOO
            bbb
            #endif
            ccc");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(7, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (2, 13), (2, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenEqual(TokenKindIdentifier, "bbb", (4, 13), (4, 15), tokens[3]);
        TokenIsEOS(tokens[4]);
        TokenEqual(TokenKindIdentifier, "ccc", (6, 13), (6, 15), tokens[5]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void NestedIfs()
    {
        var (tokens, diag) = Preprocess(
            @"
            #if FOO
                aaa
                #if BAR
                    bbb
                #endif
                #if BAZ
                    ccc
                #endif
                ddd
            #endif",
            new[] { "FOO", "BAR" });

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(8, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (3, 17), (3, 19), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenEqual(TokenKindIdentifier, "bbb", (5, 21), (5, 23), tokens[3]);
        TokenIsEOS(tokens[4]);
        TokenEqual(TokenKindIdentifier, "ddd", (10, 17), (10, 19), tokens[5]);
        TokenIsEOS(tokens[6]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void NestedIfsDisabled()
    {
        var (tokens, diag) = Preprocess(
            @"
            #if FOO
                aaa
                #if BAR
                    bbb
                #endif
                ccc
            #endif");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(2, tokens.Length); // EOS+EOF

        TokenIsEOS(tokens[0]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void InlineIfIsAllowed()
    {
        var (tokens, diag) = Preprocess(
            @"
            aaa #if FOO bbb ccc #endif ddd
            eee #if NOT FOO fff ggg #endif hhh",
            new[] { "FOO" });

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(9, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (2, 13), (2, 15), tokens[1]);
        TokenEqual(TokenKindIdentifier, "bbb", (2, 25), (2, 27), tokens[2]);
        TokenEqual(TokenKindIdentifier, "ccc", (2, 29), (2, 31), tokens[3]);
        TokenEqual(TokenKindIdentifier, "ddd", (2, 40), (2, 42), tokens[4]);
        TokenIsEOS(tokens[5]);
        TokenEqual(TokenKindIdentifier, "eee", (3, 13), (3, 15), tokens[6]);
        TokenEqual(TokenKindIdentifier, "hhh", (3, 44), (3, 46), tokens[7]);
        TokenIsEOF(tokens.Last());
    }

    private static void CheckError(TErrorCode expectedError, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, DiagnosticsReport diagnostics)
    {
        var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
        Equal(1, diagnostics.Errors.Count(err => err.Code == expectedError.AsInteger<TErrorCode, int>() && err.Source == expectedLocation));
    }

    private static void TokenEqual(TTokenKind expectedKind, string expectedContents, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, TToken token)
    {
        var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
        Equal(expectedKind, token.Kind);
        Equal(expectedContents, token.Lexeme.ToString());
        Equal(expectedLocation, token.Location);
    }
    private void TokenIsEOF(TToken token) => TokenEqual(TokenKindEOF, "", (0, 0), (0, 0), token);
    private void TokenIsEOS(TToken token) => Equal(TokenKindEOS, token.Kind);

    private const string TestFileName = "preprocessor_tests.sc";

    private static SourceRange MakeSourceRange((int Line, int Column) start, (int Line, int Column) end)
        => new((start.Line, start.Column, TestFileName), (end.Line, end.Column, TestFileName));

    private (TToken[] Tokens, DiagnosticsReport Diagnostics) Preprocess(string source, string[]? definitions = null)
    {
        var d = new DiagnosticsReport();
        var l = CreateLexer(TestFileName, source, d);
        var p = CreatePreprocessor(d);
        definitions?.ForEach(def => p.Define(def));
        return (p.Preprocess(l).ToArray(), d);
    }
}

public class PreprocessorTestsForScriptAssembly
    : PreprocessorTests<ScTools.ScriptAssembly.Token,
                        ScTools.ScriptAssembly.TokenKind,
                        ScTools.ScriptAssembly.ErrorCode>
{
    protected override ScTools.ScriptAssembly.TokenKind TokenKindIdentifier => ScTools.ScriptAssembly.TokenKind.Identifier;
    protected override ScTools.ScriptAssembly.TokenKind TokenKindEOS => ScTools.ScriptAssembly.TokenKind.EOS;
    protected override ScTools.ScriptAssembly.TokenKind TokenKindEOF => ScTools.ScriptAssembly.TokenKind.EOF;
    protected override ScTools.ScriptAssembly.Lexer CreateLexer(string filePath, string source, DiagnosticsReport diagnostics)
        => new(filePath, source, diagnostics);
    protected override ScTools.ScriptAssembly.Preprocessor CreatePreprocessor(DiagnosticsReport diagnostics)
        => new(diagnostics);
}

public class PreprocessorTestsForScriptLang
    : PreprocessorTests<ScTools.ScriptLang.Token,
                        ScTools.ScriptLang.TokenKind,
                        ScTools.ScriptLang.ErrorCode>
{
    protected override ScTools.ScriptLang.TokenKind TokenKindIdentifier => ScTools.ScriptLang.TokenKind.Identifier;
    protected override ScTools.ScriptLang.TokenKind TokenKindEOS => ScTools.ScriptLang.TokenKind.EOS;
    protected override ScTools.ScriptLang.TokenKind TokenKindEOF => ScTools.ScriptLang.TokenKind.EOF;
    protected override ScTools.ScriptLang.Lexer CreateLexer(string filePath, string source, DiagnosticsReport diagnostics)
        => new(filePath, source, diagnostics);
    protected override ScTools.ScriptLang.Preprocessor CreatePreprocessor(DiagnosticsReport diagnostics)
        => new(diagnostics);
}

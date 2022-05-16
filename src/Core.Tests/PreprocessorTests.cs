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
    public void IfDefWithDefined()
    {
        var (tokens, diag) = Preprocess(
            @"
            aaa
            #ifdef FOO
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
    public void IfDefWithUndefined()
    {
        var (tokens, diag) = Preprocess(
            @"
            aaa
            #ifdef FOO
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
    public void IfNotDefWithDefined()
    {
        var (tokens, diag) = Preprocess(
            @"
            aaa
            #ifndef FOO
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
    public void IfNotDefWithUndefined()
    {
        var (tokens, diag) = Preprocess(
            @"
            aaa
            #ifndef FOO
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
    public void ElseBranchTaken()
    {
        var (tokens, diag) = Preprocess(
            @"
            #ifdef FOO
            aaa
            #else
            bbb
            #endif");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "bbb", (5, 13), (5, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void ElseBranchNotTaken()
    {
        var (tokens, diag) = Preprocess(
            @"
            #ifndef FOO
            ccc
            #else
            ddd
            #endif");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "ccc", (3, 13), (3, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void ElifBranchTaken()
    {
        var (tokens, diag) = Preprocess(
            @"
            #ifdef FOO
            aaa
            #elifdef BAR
            bbb
            #elifdef BAZ
            ccc
            #else
            ddd
            #endif",
            new[] { "BAR" });

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "bbb", (5, 13), (5, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void ElifBranchNotTaken()
    {
        var (tokens, diag) = Preprocess(
            @"
            #ifdef FOO
            aaa
            #elifdef BAR
            bbb
            #elifdef BAZ
            ccc
            #else
            ddd
            #endif");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "ddd", (9, 13), (9, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void ElifNotBranchTaken()
    {
        var (tokens, diag) = Preprocess(
            @"
            #ifdef FOO
            aaa
            #elifndef BAR
            bbb
            #elifndef BAZ
            ccc
            #else
            ddd
            #endif");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "bbb", (5, 13), (5, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void ElifNotBranchNotTaken()
    {
        var (tokens, diag) = Preprocess(
            @"
            #ifdef FOO
            aaa
            #elifndef BAR
            bbb
            #elifndef BAZ
            ccc
            #else
            ddd
            #endif",
            new[] { "BAR", "BAZ" });

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "ddd", (9, 13), (9, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenIsEOF(tokens.Last());
    }

    [Theory]
    [InlineData("FOO")]
    [InlineData(null)]
    public void Undef(string? definition)
    {
        var (tokens, diag) = Preprocess(
            @"
            #undef FOO
            #ifdef FOO
            aaa
            #endif
            #ifndef FOO
            bbb
            #endif",
            definition is null ? null : new[] { definition });

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "bbb", (7, 13), (7, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenIsEOF(tokens.Last());
    }

    [Theory]
    [InlineData("FOO")]
    [InlineData(null)]
    public void Define(string? definition)
    {
        var (tokens, diag) = Preprocess(
            @"
            #define FOO
            #ifdef FOO
            aaa
            #endif
            #ifndef FOO
            bbb
            #endif",
            definition is null ? null : new[] { definition });

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (4, 13), (4, 15), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void NestedIfsEnabled()
    {
        var (tokens, diag) = Preprocess(
            @"
            #ifdef FOO
                aaa
                #ifdef BAR
                    bbb
                #endif
                #ifdef BAZ
                    ccc
                #elifdef BAR
                    ddd
                #else
                    eee
                #endif
                fff
            #endif",
            new[] { "FOO", "BAR" });

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(10, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (3, 17), (3, 19), tokens[1]);
        TokenIsEOS(tokens[2]);
        TokenEqual(TokenKindIdentifier, "bbb", (5, 21), (5, 23), tokens[3]);
        TokenIsEOS(tokens[4]);
        TokenEqual(TokenKindIdentifier, "ddd", (10, 21), (10, 23), tokens[5]);
        TokenIsEOS(tokens[6]);
        TokenEqual(TokenKindIdentifier, "fff", (14, 17), (14, 19), tokens[7]);
        TokenIsEOS(tokens[8]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void NestedIfsDisabled()
    {
        var (tokens, diag) = Preprocess(
            @"
            #ifdef FOO
                aaa
                #ifdef BAR
                    bbb
                #elifdef BAZ
                    ccc
                #else
                    ddd
                #endif
                eee
            #endif");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(2, tokens.Length); // EOS+EOF

        TokenIsEOS(tokens[0]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void SpacesBeforeDirectiveNameAreAllowed()
    {
        var (tokens, diag) = Preprocess(
            @"
            #   undef FOO");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(2, tokens.Length); // EOS+EOF

        TokenIsEOS(tokens[0]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void MacroExpansion()
    {
        var (tokens, diag) = Preprocess(
            @"
            #define FOO aaa

            FOO FOO");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (2, 25), (2, 27), tokens[1]);
        TokenEqual(TokenKindIdentifier, "aaa", (2, 25), (2, 27), tokens[2]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void MacroExpansionWithLineContinuation()
    {
        var (tokens, diag) = Preprocess(
            @"
            #define FOO aaa\
                bbb

            FOO");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (2, 25), (2, 27), tokens[1]);
        TokenEqual(TokenKindIdentifier, "bbb", (3, 17), (3, 19), tokens[2]);
        TokenIsEOF(tokens.Last());
    }

    [Fact]
    public void MacroInsideMacroExpansion()
    {
        var (tokens, diag) = Preprocess(
            @"
            #define FOO BAR
            #define BAR aaa FOO

            BAR");

        False(diag.HasErrors);
        False(diag.HasWarnings);

        Equal(4, tokens.Length);

        TokenIsEOS(tokens[0]);
        TokenEqual(TokenKindIdentifier, "aaa", (3, 25), (3, 27), tokens[1]);
        TokenEqual(TokenKindIdentifier, "BAR", (2, 25), (2, 27), tokens[2]);
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

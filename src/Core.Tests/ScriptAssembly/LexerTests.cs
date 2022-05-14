namespace ScTools.Tests.ScriptAssembly
{
    using System;
    using System.IO;
    using System.Linq;

    using ScTools.ScriptAssembly;

    using Xunit;
    using static Xunit.Assert;

    public class LexerTests
    {
        [Fact]
        public void Empty()
        {
            var (tokens, diag) = Lex("");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Single(tokens);

            TokenIsEOF(tokens.Single());
        }

        [Fact]
        public void Symbols()
        {
            var (tokens, diag) = Lex(".,():");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(6, tokens.Length);

            int checkTokenIndex = 0;
            (int Line, int Column) checkStart, checkEnd;
            checkStart = (1, 1);
            var check = (TokenKind expectedKind, string expectedContents) =>
            {
                checkEnd = (checkStart.Line, checkStart.Column + expectedContents.Length - 1);
                TokenEqual(expectedKind, expectedContents, checkStart, checkEnd, tokens[checkTokenIndex]);

                checkStart = (checkEnd.Line, checkEnd.Column + 1);
                checkTokenIndex++;
            };

            check(TokenKind.Dot, ".");
            check(TokenKind.Comma, ",");
            check(TokenKind.OpenParen, "(");
            check(TokenKind.CloseParen, ")");
            check(TokenKind.Colon, ":");

            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void DoubleQuoteStrings()
        {
            var (tokens, diag) = Lex(@"""hello""""world""  ""test""");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.String, @"""hello""", (1, 1), (1, 7), tokens[0]);
            TokenEqual(TokenKind.String, @"""world""", (1, 8), (1, 14), tokens[1]);
            TokenEqual(TokenKind.String, @"""test""", (1, 17), (1, 22), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void SingleQuoteStrings()
        {
            var (tokens, diag) = Lex("'hello''world'  'test'");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.String, "'hello'", (1, 1), (1, 7), tokens[0]);
            TokenEqual(TokenKind.String, "'world'", (1, 8), (1, 14), tokens[1]);
            TokenEqual(TokenKind.String, "'test'", (1, 17), (1, 22), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void HashStrings()
        {
            var (tokens, diag) = Lex("`hello``world`  `te\\nst`");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Integer, "`hello`", (1, 1), (1, 7), tokens[0]);
            TokenEqual(TokenKind.Integer, "`world`", (1, 8), (1, 14), tokens[1]);
            TokenEqual(TokenKind.Integer, "`te\\nst`", (1, 17), (1, 24), tokens[2]);
            Equal(unchecked((int)"hello".ToLowercaseHash()), tokens[0].GetIntLiteral());
            Equal(unchecked((int)"world".ToLowercaseHash()), tokens[1].GetIntLiteral());
            Equal(unchecked((int)"te\nst".ToLowercaseHash()), tokens[2].GetIntLiteral());
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void StringWithEscapeSequences()
        {
            var (tokens, diag) = Lex(@"'foo\'""`ba\nr' ""foo'\""`ba\tr"" `foo'""\`ba\rr`");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.String, @"'foo\'""`ba\nr'", (1, 1), (1, 14), tokens[0]);
            TokenEqual(TokenKind.String, @"""foo'\""`ba\tr""", (1, 16), (1, 29), tokens[1]);
            TokenEqual(TokenKind.Integer, @"`foo'""\`ba\rr`", (1, 31), (1, 44), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void IncompleteStringWithEscapeSequences()
        {
            var (tokens, diag) = Lex(@"'foo\\''"); // chars \\ shouldn't escape the following '

            True(diag.HasErrors);
            False(diag.HasWarnings);
            Single(diag.Errors);

            CheckError(ErrorCode.LexerIncompleteString, (1, 8), (1, 8), diag);

            Equal(3, tokens.Length);

            TokenEqual(TokenKind.String, @"'foo\\'", (1, 1), (1, 7), tokens[0]);
            TokenEqual(TokenKind.Bad, @"'", (1, 8), (1, 8), tokens[1]); // incomplete string
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void StringWithUnrecognizedEscapeSequences()
        {
            var (tokens, diag) = Lex(@"'\d''\ '");

            True(diag.HasErrors);
            False(diag.HasWarnings);
            Equal(2, diag.Errors.Length);

            CheckError(ErrorCode.LexerUnrecognizedEscapeSequence, (1, 2), (1, 3), diag);
            CheckError(ErrorCode.LexerUnrecognizedEscapeSequence, (1, 6), (1, 7), diag);

            Equal(3, tokens.Length);

            TokenEqual(TokenKind.String, @"'\d'", (1, 1), (1, 4), tokens[0]);
            TokenEqual(TokenKind.String, @"'\ '", (1, 5), (1, 8), tokens[1]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void Identifiers()
        {
            var (tokens, diag) = Lex("my_var  _otherVar123 f");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Identifier, "my_var", (1, 1), (1, 6), tokens[0]);
            TokenEqual(TokenKind.Identifier, "_otherVar123", (1, 9), (1, 20), tokens[1]);
            TokenEqual(TokenKind.Identifier, "f", (1, 22), (1, 22), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void DecimalIntegerLiteral()
        {
            var (tokens, diag) = Lex("0 123 000");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Integer, "0", (1, 1), (1, 1), tokens[0]);
            TokenEqual(TokenKind.Integer, "123", (1, 3), (1, 5), tokens[1]);
            TokenEqual(TokenKind.Integer, "000", (1, 7), (1, 9), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void HexadecimalIntegerLiteral()
        {
            var (tokens, diag) = Lex("0x0 0x123ABC 0x0000");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Integer, "0x0", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.Integer, "0x123ABC", (1, 5), (1, 12), tokens[1]);
            TokenEqual(TokenKind.Integer, "0x0000", (1, 14), (1, 19), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void InvalidHexadecimalIntegerLiteral()
        {
            var (tokens, diag) = Lex("0x");

            True(diag.HasErrors);
            False(diag.HasWarnings);
            Single(diag.Errors);

            CheckError(ErrorCode.LexerInvalidIntegerLiteral, (1, 1), (1, 2), diag);

            Equal(2, tokens.Length);

            TokenEqual(TokenKind.Bad, "0x", (1, 1), (1, 2), tokens[0]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void IntegerLiteralWithSign()
        {
            // positive/negative sign is included with integer
            var (tokens, diag) = Lex("+9 -9 +0x1 -0x1");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(5, tokens.Length);

            TokenEqual(TokenKind.Integer, "+9", (1, 1), (1, 2), tokens[0]);
            TokenEqual(TokenKind.Integer, "-9", (1, 4), (1, 5), tokens[1]);
            TokenEqual(TokenKind.Integer, "+0x1", (1, 7), (1, 10), tokens[2]);
            TokenEqual(TokenKind.Integer, "-0x1", (1, 12), (1, 15), tokens[3]);
            TokenIsEOF(tokens.Last());

            Equal(9, tokens[0].GetIntLiteral());
            Equal(-9, tokens[1].GetIntLiteral());
            Equal(1, tokens[2].GetIntLiteral());
            Equal(-1, tokens[3].GetIntLiteral());
        }

        [Fact]
        public void IntegersWithSignWithoutSpaces()
        {
            var (tokens, diag) = Lex("+9-9");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(3, tokens.Length);

            TokenEqual(TokenKind.Integer, "+9", (1, 1), (1, 2), tokens[0]);
            TokenEqual(TokenKind.Integer, "-9", (1, 3), (1, 4), tokens[1]);
            TokenIsEOF(tokens.Last());

            Equal(9, tokens[0].GetIntLiteral());
            Equal(-9, tokens[1].GetIntLiteral());
        }

        [Fact]
        public void FloatLiteral()
        {
            var (tokens, diag) = Lex("0.0 15.25");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(3, tokens.Length);

            TokenEqual(TokenKind.Float, "0.0", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.Float, "15.25", (1, 5), (1, 9), tokens[1]);
            TokenIsEOF(tokens.Last());

            Equal(0.0f, tokens[0].GetFloatLiteral());
            Equal(15.25f, tokens[1].GetFloatLiteral());
        }

        [Fact]
        public void FloatLiteralWithSign()
        {
            var (tokens, diag) = Lex("+0.0 -0.0 +15.25 -15.25");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(5, tokens.Length);

            TokenEqual(TokenKind.Float, "+0.0", (1, 1), (1, 4), tokens[0]);
            TokenEqual(TokenKind.Float, "-0.0", (1, 6), (1, 9), tokens[1]);
            TokenEqual(TokenKind.Float, "+15.25", (1, 11), (1, 16), tokens[2]);
            TokenEqual(TokenKind.Float, "-15.25", (1, 18), (1, 23), tokens[3]);
            TokenIsEOF(tokens.Last());

            Equal(0.0f, tokens[0].GetFloatLiteral());
            Equal(-0.0f, tokens[1].GetFloatLiteral());
            Equal(15.25f, tokens[2].GetFloatLiteral());
            Equal(-15.25f, tokens[3].GetFloatLiteral());
        }

        [Fact]
        public void FloatStartingWithDotLiteral()
        {
            var (tokens, diag) = Lex(".0 .25");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(3, tokens.Length);

            TokenEqual(TokenKind.Float, ".0", (1, 1), (1, 2), tokens[0]);
            TokenEqual(TokenKind.Float, ".25", (1, 4), (1, 6), tokens[1]);
            TokenIsEOF(tokens.Last());

            Equal(.0f, tokens[0].GetFloatLiteral());
            Equal(.25f, tokens[1].GetFloatLiteral());
        }

        [Fact]
        public void FloatWithExponentLiteral()
        {
            var (tokens, diag) = Lex("0e0 1e5 1e+5 1e-5");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(5, tokens.Length);

            TokenEqual(TokenKind.Float, "0e0", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.Float, "1e5", (1, 5), (1, 7), tokens[1]);
            TokenEqual(TokenKind.Float, "1e+5", (1, 9), (1, 12), tokens[2]);
            TokenEqual(TokenKind.Float, "1e-5", (1, 14), (1, 17), tokens[3]);
            TokenIsEOF(tokens.Last());

            Equal(0e0f, tokens[0].GetFloatLiteral());
            Equal(1e5f, tokens[1].GetFloatLiteral());
            Equal(1e+5f, tokens[2].GetFloatLiteral());
            Equal(1e-5f, tokens[3].GetFloatLiteral());
        }

        [Fact]
        public void FloatWithExponentAndDotsLiteral()
        {
            var (tokens, diag) = Lex("12.5e5 1.5e+15 1.5e-15");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Float, "12.5e5", (1, 1), (1, 6), tokens[0]);
            TokenEqual(TokenKind.Float, "1.5e+15", (1, 8), (1, 14), tokens[1]);
            TokenEqual(TokenKind.Float, "1.5e-15", (1, 16), (1, 22), tokens[2]);
            TokenIsEOF(tokens.Last());

            Equal(12.5e5f, tokens[0].GetFloatLiteral());
            Equal(1.5e+15f, tokens[1].GetFloatLiteral());
            Equal(1.5e-15f, tokens[2].GetFloatLiteral());
        }

        [Fact]
        public void FloatStartingWithDotAndWithExponentLiteral()
        {
            var (tokens, diag) = Lex(".125e5 .125e+5 .125e-5");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Float, ".125e5", (1, 1), (1, 6), tokens[0]);
            TokenEqual(TokenKind.Float, ".125e+5", (1, 8), (1, 14), tokens[1]);
            TokenEqual(TokenKind.Float, ".125e-5", (1, 16), (1, 22), tokens[2]);
            TokenIsEOF(tokens.Last());

            Equal(.125e5f, tokens[0].GetFloatLiteral());
            Equal(.125e+5f, tokens[1].GetFloatLiteral());
            Equal(.125e-5f, tokens[2].GetFloatLiteral());
        }

        [Fact]
        public void InvalidFloatWithExponentLiteral()
        {
            var (tokens, diag) = Lex("1e 1.5e .8e 1e+ 1e-");

            True(diag.HasErrors);
            False(diag.HasWarnings);
            Equal(5, diag.Errors.Length);

            CheckError(ErrorCode.LexerInvalidFloatLiteral, (1, 1), (1, 2), diag);
            CheckError(ErrorCode.LexerInvalidFloatLiteral, (1, 4), (1, 7), diag);
            CheckError(ErrorCode.LexerInvalidFloatLiteral, (1, 9), (1, 11), diag);
            CheckError(ErrorCode.LexerInvalidFloatLiteral, (1, 13), (1, 15), diag);
            CheckError(ErrorCode.LexerInvalidFloatLiteral, (1, 17), (1, 19), diag);

            Equal(6, tokens.Length);

            TokenEqual(TokenKind.Bad, "1e", (1, 1), (1, 2), tokens[0]);
            TokenEqual(TokenKind.Bad, "1.5e", (1, 4), (1, 7), tokens[1]);
            TokenEqual(TokenKind.Bad, ".8e", (1, 9), (1, 11), tokens[2]);
            TokenEqual(TokenKind.Bad, "1e+", (1, 13), (1, 15), tokens[3]);
            TokenEqual(TokenKind.Bad, "1e-", (1, 17), (1, 19), tokens[4]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void MultipleLines()
        {
            var (tokens, diag) = Lex("foo\nbar");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.EOS, "\n", (1, 4), (1, 4), tokens[1]);
            TokenEqual(TokenKind.Identifier, "bar", (2, 1), (2, 3), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void MultipleEmptyLines()
        {
            var (tokens, diag) = Lex("foo\n\n\nbar");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.EOS, "\n\n\n", (1, 4), (3, 1), tokens[1]);
            TokenEqual(TokenKind.Identifier, "bar", (4, 1), (4, 3), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void MultipleEmptyLinesWithWhiteSpaces()
        {
            var (tokens, diag) = Lex("foo\n  \t\n /*comment\ncomment*/ \nbar");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.EOS, "\n  \t\n /*comment\ncomment*/ \n", (1, 4), (4, 11), tokens[1]);
            TokenEqual(TokenKind.Identifier, "bar", (5, 1), (5, 3), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void MultipleEmptyLinesWithWhiteSpacesNoEndingInNewLine()
        {
            var (tokens, diag) = Lex("foo\n  \t\n /*comment\ncomment*/bar");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.EOS, "\n  \t\n /*comment\ncomment*/", (1, 4), (4, 9), tokens[1]);
            TokenEqual(TokenKind.Identifier, "bar", (4, 10), (4, 12), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void StatementContinuation()
        {
            var (tokens, diag) = Lex("foo\\\nbar");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(3, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.Identifier, "bar", (2, 1), (2, 3), tokens[1]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void UnexpectedCharacter()
        {
            var (tokens, diag) = Lex("foo $ bar");

            True(diag.HasErrors);
            False(diag.HasWarnings);
            Single(diag.Errors);

            CheckError(ErrorCode.LexerUnexpectedCharacter, (1, 5), (1, 5), diag);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.Bad, "$", (1, 5), (1, 5), tokens[1]);
            TokenEqual(TokenKind.Identifier, "bar", (1, 7), (1, 9), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void UnexpectedCharacterOnStatementContinuationWithoutNewLine()
        {
            var (tokens, diag) = Lex("foo \\ bar");

            True(diag.HasErrors);
            False(diag.HasWarnings);
            Single(diag.Errors);

            CheckError(ErrorCode.LexerUnexpectedCharacter, (1, 5), (1, 5), diag);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.Bad, "\\", (1, 5), (1, 5), tokens[1]);
            TokenEqual(TokenKind.Identifier, "bar", (1, 7), (1, 9), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void SingleLineComment()
        {
            var (tokens, diag) = Lex("foo//comment\nbar");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.EOS, "\n", (1, 13), (1, 13), tokens[1]);
            TokenEqual(TokenKind.Identifier, "bar", (2, 1), (2, 3), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void SingleLineCommentWithStatementContinuation()
        {
            var (tokens, diag) = Lex("foo//comment\\\nbar\nxyz"); // 'bar' should be part of the comment but not 'xyz'

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.EOS, "\n", (2, 4), (2, 4), tokens[1]);
            TokenEqual(TokenKind.Identifier, "xyz", (3, 1), (3, 3), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void MultiLineComment()
        {
            var (tokens, diag) = Lex("foo/*comment\nbar\nbaz\n*/xyz"); // the new-lines are all inside the comment so no EOS token should be produced

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(3, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.Identifier, "xyz", (4, 3), (4, 5), tokens[1]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void MultiLineCommentInSingleLine()
        {
            var (tokens, diag) = Lex("foo/*comment*/xyz");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(3, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.Identifier, "xyz", (1, 15), (1, 17), tokens[1]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void MultiLineCommentWithStatementContinuation()
        {
            var (tokens, diag) = Lex("foo/*comment\\\nbar*/xyz"); // the statement continuation should be ignored in this case. But, as the new line is inside the comment, no EOS token should be produced

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(3, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenEqual(TokenKind.Identifier, "xyz", (2, 6), (2, 8), tokens[1]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void UnfinishedMultiLineComment()
        {
            var (tokens, diag) = Lex("foo/*comment");

            True(diag.HasErrors);
            False(diag.HasWarnings);
            Single(diag.Errors);

            CheckError(ErrorCode.LexerOpenComment, (1, 4), (1, 12), diag);

            Equal(2, tokens.Length);

            TokenEqual(TokenKind.Identifier, "foo", (1, 1), (1, 3), tokens[0]);
            TokenIsEOF(tokens.Last());
        }

        private static void CheckError(ErrorCode expectedError, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, DiagnosticsReport diagnostics)
        {
            var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
            Equal(1, diagnostics.Errors.Count(err => err.Code == (int)expectedError && err.Source == expectedLocation));
        }

        private static void TokenEqual(TokenKind expectedKind, string expectedContents, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, Token token)
        {
            var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
            Equal(expectedKind, token.Kind);
            Equal(expectedContents, token.Lexeme.ToString());
            Equal(expectedLocation, token.Location);
        }
        private static void TokenIsEOF(Token token) => TokenEqual(TokenKind.EOF, "", (0, 0), (0, 0), token);

        private const string TestFileName = "lexer_tests.sc";

        private static SourceRange MakeSourceRange((int Line, int Column) start, (int Line, int Column) end)
            => new((start.Line, start.Column, TestFileName), (end.Line, end.Column, TestFileName));

        private static (Token[] Tokens, DiagnosticsReport Diagnostics) Lex(string source)
        {
            Lexer l = new(TestFileName, source, new DiagnosticsReport());
            
            return (l.ToArray(), l.Diagnostics);
        }
    }
}

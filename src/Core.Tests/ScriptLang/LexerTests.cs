namespace ScTools.Tests.ScriptLang
{
    using System;
    using System.IO;
    using System.Linq;

    using ScTools.ScriptLang;

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
            var (tokens, diag) = Lex(".,()[]=+-*/%&^|+=-=*=/=%=&=^=|=< > <= >= == <> <<  >>:");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(33, tokens.Length);

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
            var checkSkip = (int charsToSkip) => checkStart = (checkStart.Line, checkStart.Column + charsToSkip);

            check(TokenKind.Dot, ".");
            check(TokenKind.Comma, ",");
            check(TokenKind.OpenParen, "(");
            check(TokenKind.CloseParen, ")");
            check(TokenKind.OpenBracket, "[");
            check(TokenKind.CloseBracket, "]");
            check(TokenKind.Equals, "=");
            check(TokenKind.Plus, "+");
            check(TokenKind.Minus, "-");
            check(TokenKind.Asterisk, "*");
            check(TokenKind.Slash, "/");
            check(TokenKind.Percent, "%");
            check(TokenKind.Ampersand, "&");
            check(TokenKind.Caret, "^");
            check(TokenKind.Bar, "|");
            check(TokenKind.PlusEquals, "+=");
            check(TokenKind.MinusEquals, "-=");
            check(TokenKind.AsteriskEquals, "*=");
            check(TokenKind.SlashEquals, "/=");
            check(TokenKind.PercentEquals, "%=");
            check(TokenKind.AmpersandEquals, "&=");
            check(TokenKind.CaretEquals, "^=");
            check(TokenKind.BarEquals, "|=");
            check(TokenKind.LessThan, "<");
            checkSkip(1);
            check(TokenKind.GreaterThan, ">");
            checkSkip(1);
            check(TokenKind.LessThanEquals, "<=");
            checkSkip(1);
            check(TokenKind.GreaterThanEquals, ">=");
            checkSkip(1);
            check(TokenKind.EqualsEquals, "==");
            checkSkip(1);
            check(TokenKind.LessThanGreaterThan, "<>");
            checkSkip(1);
            check(TokenKind.LessThanLessThan, "<<");
            checkSkip(2);
            check(TokenKind.GreaterThanGreaterThan, ">>");
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
            var (tokens, diag) = Lex("`hello``world`  `test`");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.HashString, "`hello`", (1, 1), (1, 7), tokens[0]);
            TokenEqual(TokenKind.HashString, "`world`", (1, 8), (1, 14), tokens[1]);
            TokenEqual(TokenKind.HashString, "`test`", (1, 17), (1, 22), tokens[2]);
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
            TokenEqual(TokenKind.HashString, @"`foo'""\`ba\rr`", (1, 31), (1, 44), tokens[2]);
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
        public void Keywords()
        {
            foreach (var tokenKind in Enum.GetValues<TokenKind>())
            {
                if (!tokenKind.IsKeyword())
                    continue;

                var keyword = tokenKind.ToString();
                var keywordUp = keyword.ToUpperInvariant();
                var keywordLo = keyword.ToLowerInvariant();

                var (tokens, diag) = Lex($"{keywordUp} {keywordLo}");

                False(diag.HasErrors);
                False(diag.HasWarnings);

                Equal(3, tokens.Length);

                TokenEqual(tokenKind, keywordUp, (1, 1), (1, keywordUp.Length), tokens[0]);
                TokenEqual(tokenKind, keywordLo, (1, keywordUp.Length + 2), (1, keywordUp.Length + 1 + keywordLo.Length), tokens[1]);
                TokenIsEOF(tokens.Last());
            }
        }

        [Fact]
        public void Identifiers()
        {
            var (tokens, diag) = Lex("my_var  _otherVar123 foo");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Identifier, "my_var", (1, 1), (1, 6), tokens[0]);
            TokenEqual(TokenKind.Identifier, "_otherVar123", (1, 9), (1, 20), tokens[1]);
            TokenEqual(TokenKind.Identifier, "foo", (1, 22), (1, 24), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void BoolTrueLiteral()
        {
            var (tokens, diag) = Lex("TRUE TrUe true");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Boolean, "TRUE", (1, 1), (1, 4), tokens[0]);
            TokenEqual(TokenKind.Boolean, "TrUe", (1, 6), (1, 9), tokens[1]);
            TokenEqual(TokenKind.Boolean, "true", (1, 11), (1, 14), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void BoolFalseLiteral()
        {
            var (tokens, diag) = Lex("FALSE FaLsE false");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Boolean, "FALSE", (1, 1), (1, 5), tokens[0]);
            TokenEqual(TokenKind.Boolean, "FaLsE", (1, 7), (1, 11), tokens[1]);
            TokenEqual(TokenKind.Boolean, "false", (1, 13), (1, 17), tokens[2]);
            TokenIsEOF(tokens.Last());
        }

        [Fact]
        public void NullLiteral()
        {
            var (tokens, diag) = Lex("NULL NuLl null");

            False(diag.HasErrors);
            False(diag.HasWarnings);

            Equal(4, tokens.Length);

            TokenEqual(TokenKind.Null, "NULL", (1, 1), (1, 4), tokens[0]);
            TokenEqual(TokenKind.Null, "NuLl", (1, 6), (1, 9), tokens[1]);
            TokenEqual(TokenKind.Null, "null", (1, 11), (1, 14), tokens[2]);
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

        private static void CheckError(ErrorCode expectedError, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, DiagnosticsReport diagnostics)
        {
            var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
            Equal(1, diagnostics.Errors.Count(err => err.Code == (int)expectedError && err.Source == expectedLocation));
        }

        private static void TokenEqual(TokenKind expectedKind, string expectedContents, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, Token token)
        {
            var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
            Equal(expectedKind, token.Kind);
            Equal(expectedContents, token.Contents.ToString());
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

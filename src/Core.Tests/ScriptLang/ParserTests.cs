namespace ScTools.Tests.ScriptLang
{
    using System;
    using System.IO;
    using System.Linq;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;

    using Xunit;
    using static Xunit.Assert;

    public class ParserTests
    {
        [Fact]
        public void Label()
        {
            var p = ParserFor(
                "my_label:"
            );

            True(p.IsPossibleLabel());
            var label = p.ParseLabel();
            True(p.IsAtEOF);

            Equal("my_label", label);
        }

        [Fact]
        public void BasicStatements()
        {
            var p = ParserFor(
                @"BREAK
                  RETURN
                  GOTO hello
                  hello: CONTINUE
                  world:
                  BREAK"
            );

            Assert(p.ParseLabeledStatement(), n => n is BreakStatement { Label: null });
            Assert(p.ParseLabeledStatement(), n => n is ReturnStatement { Label: null });
            Assert(p.ParseLabeledStatement(), n => n is GotoStatement { Label: null, TargetLabel: "hello" });
            Assert(p.ParseLabeledStatement(), n => n is ContinueStatement { Label: "hello" });
            Assert(p.ParseLabeledStatement(), n => n is BreakStatement { Label: "world" });
            True(p.IsAtEOF);
        }

        [Fact]
        public void BasicExpressions()
        {
            var p = ParserFor(
                @"hello \
                  123 \
                  12.5e2 \
                  ""hello\nworld"" \
                  True \
                  FALSE \
                  NULL"
            );

            Assert(p.ParseExpression(), n => n is DeclarationRefExpression { Name: "hello" });
            Assert(p.ParseExpression(), n => n is IntLiteralExpression { Value: 123 });
            Assert(p.ParseExpression(), n => n is FloatLiteralExpression { Value: 12.5e2f });
            Assert(p.ParseExpression(), n => n is StringLiteralExpression { Value: "hello\nworld" });
            Assert(p.ParseExpression(), n => n is BoolLiteralExpression { Value: true });
            Assert(p.ParseExpression(), n => n is BoolLiteralExpression { Value: false });
            Assert(p.ParseExpression(), n => n is NullExpression);
            True(p.IsAtEOF);
        }


        [Fact]
        public void UnaryExpressions()
        {
            var p = ParserFor(
                @"NOT hello \
                  -world"
            );

            Assert(p.ParseExpression(), n => n is UnaryExpression
            {
                Operator: UnaryOperator.LogicalNot,
                SubExpression: DeclarationRefExpression{ Name: "hello" }
            });
            Assert(p.ParseExpression(), n => n is UnaryExpression
            {
                Operator: UnaryOperator.Negate,
                SubExpression: DeclarationRefExpression { Name: "world" }
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void VectorExpression()
        {
            var p = ParserFor(
                @"<<1,2.5,<<3,4,5>>>>"
            );

            Assert(p.ParseExpression(), n => n is VectorExpression
            {
                X: IntLiteralExpression { Value: 1 },
                Y: FloatLiteralExpression { Value: 2.5f },
                Z: VectorExpression
                {
                    X: IntLiteralExpression { Value: 3 },
                    Y: IntLiteralExpression { Value: 4 },
                    Z: IntLiteralExpression { Value: 5 },
                },
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void VectorExpressionMissingComponentX()
        {
            var p = ParserFor(
                @"<<,2.5,3>>"
            );

            AssertError(p.ParseExpression(), n => n is ErrorExpression);
            CheckError(ErrorCode.ParserUnknownExpression, (1, 3), (1, 3), p.Diagnostics);
        }

        [Fact]
        public void VectorExpressionMissingComponentY()
        {
            var p = ParserFor(
                @"<<1,,3>>"
            );

            AssertError(p.ParseExpression(), n => n is ErrorExpression);
            CheckError(ErrorCode.ParserUnknownExpression, (1, 5), (1, 5), p.Diagnostics);
        }

        [Fact]
        public void VectorExpressionMissingComponentZ()
        {
            var p = ParserFor(
                @"<<1,2.5,>>"
            );
            
            AssertError(p.ParseExpression(), n => n is ErrorExpression);
            CheckError(ErrorCode.ParserUnknownExpression, (1, 9), (1, 10), p.Diagnostics);
        }

        [Fact]
        public void IncompleteVectorExpression()
        {
            var p = ParserFor(
                @"<<1,2,3"
            );

            AssertError(p.ParseExpression(), n => n is ErrorExpression);
            // TODO: this SourceRange should probably be the last correct token location
            CheckError(ErrorCode.ParserUnexpectedToken, (0, 0), (0, 0), p.Diagnostics);
        }

        [Fact]
        public void ParenthesizedExpression()
        {
            var p = ParserFor(
                @"((25))"
            );

            Assert(p.ParseExpression(), n => n is IntLiteralExpression { Value: 25 });
            True(p.IsAtEOF);
        }




        private static void Assert(INode node, Predicate<INode> predicate)
        {
            False(node is IError);
            True(predicate(node));
        }
        private static void AssertError(INode node, Predicate<INode> predicate)
        {
            True(node is IError);
            True(predicate(node));
        }

        private static void CheckError(ErrorCode expectedError, (int Line, int Column) expectedStart, (int Line, int Column) expectedEnd, DiagnosticsReport diagnostics)
        {
            var expectedLocation = MakeSourceRange(expectedStart, expectedEnd);
            Equal(1, diagnostics.Errors.Count(err => err.Code == (int)expectedError && err.Source == expectedLocation));
        }

        private const string TestFileName = "parser_tests.sc";

        private static SourceRange MakeSourceRange((int Line, int Column) start, (int Line, int Column) end)
            => new((start.Line, start.Column, TestFileName), (end.Line, end.Column, TestFileName));

        private static ParserNew ParserFor(string source)
        {
            var lexer = new Lexer(TestFileName, source, new DiagnosticsReport());
            return new(lexer, lexer.Diagnostics);
        }
    }
}

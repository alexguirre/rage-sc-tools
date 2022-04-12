namespace ScTools.Tests.ScriptLang
{
    using System;
    using System.IO;
    using System.Linq;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;

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
        public void VarDeclarationStatement()
        {
            var p = ParserFor(
                @"INT hello
                  label: FLOAT foo, bar"
            );

            Assert(p.ParseLabeledStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "hello", IsReference: false, Type: NamedType { Name: "INT" }
            });
            Assert(p.ParseLabeledStatement(), n => n is VarDeclaration
            {
                Label: "label", Name: "foo", IsReference: false, Type: NamedType { Name: "FLOAT" }
            });
            Assert(p.ParseLabeledStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "bar", IsReference: false, Type: NamedType { Name: "FLOAT" }
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void VarDeclarationStatementWithArrayType()
        {
            var p = ParserFor(
                @"INT hello[5]
                  label: FLOAT foo[2], bar[]"
            );

            Assert(p.ParseLabeledStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "hello", IsReference: false,
                Type: ArrayType_New { ItemType: NamedType { Name: "INT" }, RankExpression: IntLiteralExpression { Value: 5 } }
            });
            Assert(p.ParseLabeledStatement(), n => n is VarDeclaration
            {
                Label: "label", Name: "foo", IsReference: false,
                Type: ArrayType_New { ItemType: NamedType { Name: "FLOAT" }, RankExpression: IntLiteralExpression { Value: 2 } }
            });
            Assert(p.ParseLabeledStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "bar", IsReference: false,
                Type: IncompleteArrayType { ItemType: NamedType { Name: "FLOAT" } }
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void VarDeclarationStatementWithMultiDimensionalArrayType()
        {
            var p = ParserFor(
                @"INT hello[1][2]
                  FLOAT foo[2][3][5]"
            );

            Assert(p.ParseLabeledStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "hello", IsReference: false,
                Type: ArrayType_New
                {
                    RankExpression: IntLiteralExpression { Value: 1 },
                    ItemType: ArrayType_New
                    {
                        RankExpression: IntLiteralExpression { Value: 2 },
                        ItemType: NamedType { Name: "INT" },
                    },
                }
            });
            Assert(p.ParseLabeledStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "foo", IsReference: false,
                Type: ArrayType_New
                {
                    RankExpression: IntLiteralExpression { Value: 2 },
                    ItemType: ArrayType_New
                    {
                        RankExpression: IntLiteralExpression { Value: 3 },
                        ItemType: ArrayType_New
                        {
                            RankExpression: IntLiteralExpression { Value: 5 },
                            ItemType: NamedType { Name: "FLOAT" },
                        },
                    },
                }
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void VarDeclarationStatementWithRefType()
        {
            var p = ParserFor(
                @"INT &hello
                  FLOAT &foo"
            );

            Assert(p.ParseLabeledStatement(), n => n is VarDeclaration
            { 
                Label: null, Name: "hello", IsReference: true, Type: NamedType { Name: "INT" }
            });
            Assert(p.ParseLabeledStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "foo", IsReference: true, Type: NamedType { Name: "FLOAT" }
            });
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

        [Fact]
        public void FieldAccess()
        {
            var p = ParserFor(
                @"foo.bar.baz"
            );

            Assert(p.ParseExpression(), n => n is FieldAccessExpression
            {
                FieldName: "baz",
                SubExpression: FieldAccessExpression
                {
                    FieldName: "bar",
                    SubExpression: DeclarationRefExpression { Name: "foo" }
                }
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void ArithmeticExpressionPrecedence()
        {
            var p = ParserFor(
                @"2 * -3 + 4"
            );

            Assert(p.ParseExpression(), n => n is BinaryExpression
            {
                Operator: BinaryOperator.Add,
                LHS: BinaryExpression
                { 
                    Operator: BinaryOperator.Multiply,
                    LHS: IntLiteralExpression { Value: 2 },
                    RHS: UnaryExpression
                    {
                        Operator: UnaryOperator.Negate,
                        SubExpression: IntLiteralExpression { Value: 3 }
                    },
                },
                RHS: IntLiteralExpression { Value: 4 },
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void ArithmeticExpressionPrecedenceWithParenthesis()
        {
            var p = ParserFor(
                @"2 * (-3 + 4)"
            );

            Assert(p.ParseExpression(), n => n is BinaryExpression
            {
                Operator: BinaryOperator.Multiply,
                LHS: IntLiteralExpression { Value: 2 },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.Add,
                    LHS: UnaryExpression
                    {
                        Operator: UnaryOperator.Negate,
                        SubExpression: IntLiteralExpression { Value: 3 }
                    },
                    RHS: IntLiteralExpression { Value: 4 },
                },
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void LogicalExpressionPrecedence()
        {
            var p = ParserFor(
                @"NOT a AND b OR c OR d == e"
            );

            // (((NOT a) AND b) OR c) OR (d == e)
            Assert(p.ParseExpression(), n => n is BinaryExpression
            {
                Operator: BinaryOperator.LogicalOr,
                LHS: BinaryExpression
                {
                    Operator: BinaryOperator.LogicalOr,
                    LHS: BinaryExpression
                    {
                        Operator: BinaryOperator.LogicalAnd,
                        LHS: UnaryExpression
                        {
                            Operator: UnaryOperator.LogicalNot,
                            SubExpression: DeclarationRefExpression { Name: "a" }
                        },
                        RHS: DeclarationRefExpression { Name: "b" },
                    },
                    RHS: DeclarationRefExpression { Name: "c" },
                },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.Equals,
                    LHS: DeclarationRefExpression { Name: "d" },
                    RHS: DeclarationRefExpression { Name: "e" },
                },
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void LogicalExpressionPrecedenceWithParenthesis()
        {
            var p = ParserFor(
                @"NOT (a AND b) OR (c OR d) == e"
            );

            // (NOT (a AND b)) OR ((c OR d) == e)
            Assert(p.ParseExpression(), n => n is BinaryExpression
            {
                Operator: BinaryOperator.LogicalOr,
                LHS: UnaryExpression
                {
                    Operator: UnaryOperator.LogicalNot,
                    SubExpression: BinaryExpression
                    {
                        Operator: BinaryOperator.LogicalAnd,
                        LHS: DeclarationRefExpression { Name: "a" },
                        RHS: DeclarationRefExpression { Name: "b" },
                    }
                },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.Equals,
                    LHS: BinaryExpression
                    {
                        Operator: BinaryOperator.LogicalOr,
                        LHS: DeclarationRefExpression { Name: "c" },
                        RHS: DeclarationRefExpression { Name: "d" },
                    },
                    RHS: DeclarationRefExpression { Name: "e" },
                },
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void BitwiseExpressionPrecedence()
        {
            var p = ParserFor(
                @"2 & -3 | 4"
            );

            // (2 & -3) | 4
            Assert(p.ParseExpression(), n => n is BinaryExpression
            {
                Operator: BinaryOperator.Or,
                LHS: BinaryExpression
                {
                    Operator: BinaryOperator.And,
                    LHS: IntLiteralExpression { Value: 2 },
                    RHS: UnaryExpression
                    {
                        Operator: UnaryOperator.Negate,
                        SubExpression: IntLiteralExpression { Value: 3 }
                    },
                },
                RHS: IntLiteralExpression { Value: 4 },
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void BitwiseExpressionPrecedenceWithParenthesis()
        {
            var p = ParserFor(
                @"2 & (-3 | 4)"
            );

            Assert(p.ParseExpression(), n => n is BinaryExpression
            {
                Operator: BinaryOperator.And,
                LHS: IntLiteralExpression { Value: 2 },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.Or,
                    LHS: UnaryExpression
                    {
                        Operator: UnaryOperator.Negate,
                        SubExpression: IntLiteralExpression { Value: 3 }
                    },
                    RHS: IntLiteralExpression { Value: 4 },
                },
            });
            True(p.IsAtEOF);
        }

        // TODO: precedence of comparison operators
        // TODO: precedence of arithmetic, bitwise, comparison, logical operators combined

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

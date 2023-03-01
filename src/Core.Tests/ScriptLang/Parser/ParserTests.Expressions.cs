namespace ScTools.Tests.ScriptLang.Parser;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Errors;
using ScTools.ScriptLang.Ast.Expressions;

public partial class ParserTests
{
    public class Expressions
    {
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

            ParseExpressionAndAssert(p, n => n is NameExpression { Name: "hello" });
            ParseExpressionAndAssert(p, n => n is IntLiteralExpression { Value: 123 });
            ParseExpressionAndAssert(p, n => n is FloatLiteralExpression { Value: 12.5e2f });
            ParseExpressionAndAssert(p, n => n is StringLiteralExpression { Value: "hello\nworld" });
            ParseExpressionAndAssert(p, n => n is BoolLiteralExpression { Value: true });
            ParseExpressionAndAssert(p, n => n is BoolLiteralExpression { Value: false });
            ParseExpressionAndAssert(p, n => n is NullExpression);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void UnaryExpressions()
        {
            var p = ParserFor(
                @"NOT hello"
            );
            ParseExpressionAndAssert(p, n => n is UnaryExpression
            {
                Operator: UnaryOperator.LogicalNot,
                SubExpression: NameExpression { Name: "hello" }
            });
            NoErrorsAndIsAtEOF(p);

            p = ParserFor(
                @"-world"
            );
            ParseExpressionAndAssert(p, n => n is UnaryExpression
            {
                Operator: UnaryOperator.Negate,
                SubExpression: NameExpression { Name: "world" }
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void PostfixUnaryExpressions()
        {
            var p = ParserFor(
                @"hello++"
            );
            ParseExpressionAndAssert(p, n => n is PostfixUnaryExpression
            {
                Operator: PostfixUnaryOperator.Increment,
                SubExpression: NameExpression { Name: "hello" }
            });
            NoErrorsAndIsAtEOF(p);

            p = ParserFor(
                @"world--"
            );
            ParseExpressionAndAssert(p, n => n is PostfixUnaryExpression
            {
                Operator: PostfixUnaryOperator.Decrement,
                SubExpression: NameExpression { Name: "world" }
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void VectorExpression()
        {
            var p = ParserFor(
                @"<<1,2.5,<<3,4,5>>>>"
            );

            ParseExpressionAndAssert(p, n => n is VectorExpression
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
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void VectorExpressionMissingComponentX()
        {
            var p = ParserFor(
                @"<<,2.5,3>>"
            );

            True(p.IsPossibleExpression());
            Assert(p.ParseExpression(), n => n is VectorExpression
            {
                X: ErrorExpression,
                Y: FloatLiteralExpression { Value: 2.5f },
                Z: IntLiteralExpression { Value: 3 },
            });
            CheckError(ErrorCode.ParserUnknownExpression, (1, 3), (1, 3), p.Diagnostics);
        }

        [Fact]
        public void VectorExpressionMissingComponentY()
        {
            var p = ParserFor(
                @"<<1,,3>>"
            );

            True(p.IsPossibleExpression());
            Assert(p.ParseExpression(), n => n is VectorExpression
            {
                X: IntLiteralExpression { Value: 1 },
                Y: ErrorExpression,
                Z: IntLiteralExpression { Value: 3 },
            });
            CheckError(ErrorCode.ParserUnknownExpression, (1, 5), (1, 5), p.Diagnostics);
        }

        [Fact]
        public void VectorExpressionMissingComponentZ()
        {
            var p = ParserFor(
                @"<<1,2.5,>>"
            );

            True(p.IsPossibleExpression());
            Assert(p.ParseExpression(), n => n is VectorExpression
            {
                X: IntLiteralExpression { Value: 1 },
                Y: FloatLiteralExpression { Value: 2.5f },
                Z: ErrorExpression,
            });
            CheckError(ErrorCode.ParserUnknownExpression, (1, 9), (1, 10), p.Diagnostics);
        }

        [Fact]
        public void IncompleteVectorExpression()
        {
            var p = ParserFor(
                @"<<1,2,3"
            );

            True(p.IsPossibleExpression());
            var n = p.ParseExpression();
            Assert(n, n => n is VectorExpression
            {
                X: IntLiteralExpression { Value: 1 },
                Y: IntLiteralExpression { Value: 2 },
                Z: IntLiteralExpression { Value: 3 },
            });
            True(n.Tokens.Last() is { IsMissing: true, Kind: TokenKind.GreaterThanGreaterThan });
            // TODO: this SourceRange should probably be the last correct token location
            CheckError(ErrorCode.ParserUnexpectedToken, (0, 0), (0, 0), p.Diagnostics);
        }

        [Fact]
        public void ParenthesizedExpression()
        {
            var p = ParserFor(
                @"((25))"
            );

            ParseExpressionAndAssert(p, n => n is IntLiteralExpression { Value: 25 });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void FieldAccess()
        {
            var p = ParserFor(
                @"foo.bar.baz"
            );

            ParseExpressionAndAssert(p, n => n is FieldAccessExpression
            {
                FieldName: "baz",
                SubExpression: FieldAccessExpression
                {
                    FieldName: "bar",
                    SubExpression: NameExpression { Name: "foo" }
                }
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void FieldAccessHasHigherPrecedenceThanBinaryOperators()
        {
            var p = ParserFor(
                @"foo.bar.baz * hello . world"
            );

            ParseExpressionAndAssert(p, n => n is BinaryExpression
            {
                Operator: BinaryOperator.Multiply,
                LHS: FieldAccessExpression
                {
                    FieldName: "baz",
                    SubExpression: FieldAccessExpression
                    {
                        FieldName: "bar",
                        SubExpression: NameExpression { Name: "foo" }
                    }
                },
                RHS: FieldAccessExpression
                {
                    FieldName: "world",
                    SubExpression: NameExpression { Name: "hello" },
                },
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void Indexing()
        {
            var p = ParserFor(
                @"foo[1+2] \
                  a + b[1]"
            );

            ParseExpressionAndAssert(p, n => n is IndexingExpression
            {
                Array: NameExpression { Name: "foo" },
                Index: BinaryExpression
                {
                    Operator: BinaryOperator.Add,
                    LHS: IntLiteralExpression { Value: 1 },
                    RHS: IntLiteralExpression { Value: 2 },
                }
            });
            ParseExpressionAndAssert(p, n => n is BinaryExpression
            {
                Operator: BinaryOperator.Add,
                LHS: NameExpression { Name: "a" },
                RHS: IndexingExpression
                {
                    Array: NameExpression { Name: "b" },
                    Index: IntLiteralExpression { Value: 1 },
                }
            });
            NoErrorsAndIsAtEOF(p);

            p = ParserFor(
                @"(a + b)[1]"
            );
            ParseExpressionAndAssert(p, n => n is IndexingExpression
            {
                Array: BinaryExpression
                {
                    Operator: BinaryOperator.Add,
                    LHS: NameExpression { Name: "a" },
                    RHS: NameExpression { Name: "b" },
                },
                Index: IntLiteralExpression { Value: 1 },
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void Invocation()
        {
            var p = ParserFor(
                @"foo(1+2, a) \
                  bar() \
                  baz(0)"
            );

            True(p.IsPossibleExpression());
            AssertInvocation(p.ParseExpression(),
                callee => callee is NameExpression { Name: "foo" },
                args => Collection(args,
                    _0 => True(_0 is BinaryExpression
                    {
                        Operator: BinaryOperator.Add,
                        LHS: IntLiteralExpression { Value: 1 },
                        RHS: IntLiteralExpression { Value: 2 },
                    }),
                    _1 => True(_1 is NameExpression { Name: "a" })
                    )
                );
            True(p.IsPossibleExpression());
            AssertInvocation(p.ParseExpression(),
                callee => callee is NameExpression { Name: "bar" },
                args => Empty(args)
                );
            True(p.IsPossibleExpression());
            AssertInvocation(p.ParseExpression(),
                callee => callee is NameExpression { Name: "baz" },
                args => Collection(args,
                    _0 => True(_0 is IntLiteralExpression { Value: 0 })
                    )
                );
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ArithmeticExpressionPrecedence()
        {
            var p = ParserFor(
                @"2 * -3 + 4"
            );

            ParseExpressionAndAssert(p, n => n is BinaryExpression
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
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ArithmeticExpressionPrecedence2()
        {
            var p = ParserFor(
                @"2 + -3 * 4"
            );

            ParseExpressionAndAssert(p, n => n is BinaryExpression
            {
                Operator: BinaryOperator.Add,
                LHS: IntLiteralExpression { Value: 2 },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.Multiply,
                    LHS: UnaryExpression
                    {
                        Operator: UnaryOperator.Negate,
                        SubExpression: IntLiteralExpression { Value: 3 }
                    },
                    RHS: IntLiteralExpression { Value: 4 },
                },
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ArithmeticExpressionPrecedenceWithParenthesis()
        {
            var p = ParserFor(
                @"2 * (-3 + 4)"
            );

            ParseExpressionAndAssert(p, n => n is BinaryExpression
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
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void LogicalExpressionPrecedence()
        {
            var p = ParserFor(
                @"NOT a AND b OR c OR d == e"
            );

            // (((NOT a) AND b) OR c) OR (d == e)
            ParseExpressionAndAssert(p, n => n is BinaryExpression
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
                            SubExpression: NameExpression { Name: "a" }
                        },
                        RHS: NameExpression { Name: "b" },
                    },
                    RHS: NameExpression { Name: "c" },
                },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.Equals,
                    LHS: NameExpression { Name: "d" },
                    RHS: NameExpression { Name: "e" },
                },
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void LogicalExpressionPrecedenceWithParenthesis()
        {
            var p = ParserFor(
                @"NOT (a AND b) OR (c OR d) == e"
            );

            // (NOT (a AND b)) OR ((c OR d) == e)
            ParseExpressionAndAssert(p, n => n is BinaryExpression
            {
                Operator: BinaryOperator.LogicalOr,
                LHS: UnaryExpression
                {
                    Operator: UnaryOperator.LogicalNot,
                    SubExpression: BinaryExpression
                    {
                        Operator: BinaryOperator.LogicalAnd,
                        LHS: NameExpression { Name: "a" },
                        RHS: NameExpression { Name: "b" },
                    }
                },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.Equals,
                    LHS: BinaryExpression
                    {
                        Operator: BinaryOperator.LogicalOr,
                        LHS: NameExpression { Name: "c" },
                        RHS: NameExpression { Name: "d" },
                    },
                    RHS: NameExpression { Name: "e" },
                },
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void BitwiseExpressionPrecedence()
        {
            var p = ParserFor(
                @"2 & -3 | 4"
            );

            // (2 & -3) | 4
            ParseExpressionAndAssert(p, n => n is BinaryExpression
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
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void BitwiseExpressionPrecedenceWithParenthesis()
        {
            var p = ParserFor(
                @"2 & (-3 | 4)"
            );

            ParseExpressionAndAssert(p, n => n is BinaryExpression
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
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ComparisonExpressionPrecedence()
        {
            var p = ParserFor(
                @"a <= b <> -c > d"
            );

            // (a <= b) <> (-c > d)
            ParseExpressionAndAssert(p, n => n is BinaryExpression
            {
                Operator: BinaryOperator.NotEquals,
                LHS: BinaryExpression
                {
                    Operator: BinaryOperator.LessThanOrEqual,
                    LHS: NameExpression { Name: "a" },
                    RHS: NameExpression { Name: "b" },
                },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.GreaterThan,
                    LHS: UnaryExpression
                    {
                        Operator: UnaryOperator.Negate,
                        SubExpression: NameExpression { Name: "c" },
                    },
                    RHS: NameExpression { Name: "d" },
                },
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void CombinedOperatorsExpressionPrecedence()
        {
            var p = ParserFor(
                @"a OR b AND c | d ^ e & f == g > h + i * j"
            );

            // a OR (b AND (c | (d ^ (e & (f == (g > (h + (i * j))))))))
            ParseExpressionAndAssert(p, n => n is BinaryExpression
            {
                Operator: BinaryOperator.LogicalOr,
                LHS: NameExpression { Name: "a" },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.LogicalAnd,
                    LHS: NameExpression { Name: "b" },
                    RHS: BinaryExpression
                    {
                        Operator: BinaryOperator.Or,
                        LHS: NameExpression { Name: "c" },
                        RHS: BinaryExpression
                        {
                            Operator: BinaryOperator.Xor,
                            LHS: NameExpression { Name: "d" },
                            RHS: BinaryExpression
                            {
                                Operator: BinaryOperator.And,
                                LHS: NameExpression { Name: "e" },
                                RHS: BinaryExpression
                                {
                                    Operator: BinaryOperator.Equals,
                                    LHS: NameExpression { Name: "f" },
                                    RHS: BinaryExpression
                                    {
                                        Operator: BinaryOperator.GreaterThan,
                                        LHS: NameExpression { Name: "g" },
                                        RHS: BinaryExpression
                                        {
                                            Operator: BinaryOperator.Add,
                                            LHS: NameExpression { Name: "h" },
                                            RHS: BinaryExpression
                                            {
                                                Operator: BinaryOperator.Multiply,
                                                LHS: NameExpression { Name: "i" },
                                                RHS: NameExpression { Name: "j" },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            });
            NoErrorsAndIsAtEOF(p);
        }
    }
}

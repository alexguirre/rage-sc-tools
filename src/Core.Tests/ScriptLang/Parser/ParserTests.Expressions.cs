namespace ScTools.Tests.ScriptLang
{
    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;

    using Xunit;
    using static Xunit.Assert;

    public partial class ParserTests
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

            AssertParseExpression(p, n => n is DeclarationRefExpression { Name: "hello" });
            AssertParseExpression(p, n => n is IntLiteralExpression { Value: 123 });
            AssertParseExpression(p, n => n is FloatLiteralExpression { Value: 12.5e2f });
            AssertParseExpression(p, n => n is StringLiteralExpression { Value: "hello\nworld" });
            AssertParseExpression(p, n => n is BoolLiteralExpression { Value: true });
            AssertParseExpression(p, n => n is BoolLiteralExpression { Value: false });
            AssertParseExpression(p, n => n is NullExpression);
            True(p.IsAtEOF);
        }

        [Fact]
        public void UnaryExpressions()
        {
            var p = ParserFor(
                @"NOT hello"
            );
            AssertParseExpression(p, n => n is UnaryExpression
            {
                Operator: UnaryOperator.LogicalNot,
                SubExpression: DeclarationRefExpression{ Name: "hello" }
            });
            True(p.IsAtEOF);

            p = ParserFor(
                @"-world"
            );
            AssertParseExpression(p, n => n is UnaryExpression
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

            AssertParseExpression(p, n => n is VectorExpression
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

            True(p.IsPossibleExpression());
            AssertError(p.ParseExpression(), n => n is ErrorExpression);
            CheckError(ErrorCode.ParserUnknownExpression, (1, 3), (1, 3), p.Diagnostics);
        }

        [Fact]
        public void VectorExpressionMissingComponentY()
        {
            var p = ParserFor(
                @"<<1,,3>>"
            );

            True(p.IsPossibleExpression());
            AssertError(p.ParseExpression(), n => n is ErrorExpression);
            CheckError(ErrorCode.ParserUnknownExpression, (1, 5), (1, 5), p.Diagnostics);
        }

        [Fact]
        public void VectorExpressionMissingComponentZ()
        {
            var p = ParserFor(
                @"<<1,2.5,>>"
            );

            True(p.IsPossibleExpression());
            AssertError(p.ParseExpression(), n => n is ErrorExpression);
            CheckError(ErrorCode.ParserUnknownExpression, (1, 9), (1, 10), p.Diagnostics);
        }

        [Fact]
        public void IncompleteVectorExpression()
        {
            var p = ParserFor(
                @"<<1,2,3"
            );

            True(p.IsPossibleExpression());
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

            AssertParseExpression(p, n => n is IntLiteralExpression { Value: 25 });
            True(p.IsAtEOF);
        }

        [Fact]
        public void FieldAccess()
        {
            var p = ParserFor(
                @"foo.bar.baz"
            );

            AssertParseExpression(p, n => n is FieldAccessExpression
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
        public void Indexing()
        {
            var p = ParserFor(
                @"foo[1+2] \
                  a + b[1]"
            );

            AssertParseExpression(p, n => n is IndexingExpression
            {
                Array: DeclarationRefExpression { Name: "foo" },
                Index: BinaryExpression
                {
                    Operator: BinaryOperator.Add,
                    LHS: IntLiteralExpression { Value: 1 },
                    RHS: IntLiteralExpression { Value: 2 },
                }
            });
            AssertParseExpression(p, n => n is BinaryExpression
            {
                Operator: BinaryOperator.Add,
                LHS: DeclarationRefExpression { Name: "a" },
                RHS: IndexingExpression
                {
                    Array: DeclarationRefExpression { Name: "b" },
                    Index: IntLiteralExpression { Value: 1 },
                }
            });
            True(p.IsAtEOF);

            p = ParserFor(
                @"(a + b)[1]"
            );
            AssertParseExpression(p, n => n is IndexingExpression
            {
                Array: BinaryExpression
                {
                    Operator: BinaryOperator.Add,
                    LHS: DeclarationRefExpression { Name: "a" },
                    RHS: DeclarationRefExpression { Name: "b" },
                },
                Index: IntLiteralExpression { Value: 1 },
            });
            True(p.IsAtEOF);
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
                callee => callee is DeclarationRefExpression { Name: "foo" },
                args => Collection(args,
                    _0 => True(_0 is BinaryExpression
                    {
                        Operator: BinaryOperator.Add,
                        LHS: IntLiteralExpression { Value: 1 },
                        RHS: IntLiteralExpression { Value: 2 },
                    }),
                    _1 => True(_1 is DeclarationRefExpression { Name: "a" })
                    )
                );
            True(p.IsPossibleExpression());
            AssertInvocation(p.ParseExpression(),
                callee => callee is DeclarationRefExpression { Name: "bar" },
                args => Empty(args)
                );
            True(p.IsPossibleExpression());
            AssertInvocation(p.ParseExpression(),
                callee => callee is DeclarationRefExpression { Name: "baz" },
                args => Collection(args,
                    _0 => True(_0 is IntLiteralExpression { Value: 0 })
                    )
                );
            True(p.IsAtEOF);
        }

        [Fact]
        public void ArithmeticExpressionPrecedence()
        {
            var p = ParserFor(
                @"2 * -3 + 4"
            );

            AssertParseExpression(p, n => n is BinaryExpression
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
        public void ArithmeticExpressionPrecedence2()
        {
            var p = ParserFor(
                @"2 + -3 * 4"
            );

            AssertParseExpression(p, n => n is BinaryExpression
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
            True(p.IsAtEOF);
        }

        [Fact]
        public void ArithmeticExpressionPrecedenceWithParenthesis()
        {
            var p = ParserFor(
                @"2 * (-3 + 4)"
            );

            AssertParseExpression(p, n => n is BinaryExpression
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
            AssertParseExpression(p, n => n is BinaryExpression
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
            AssertParseExpression(p, n => n is BinaryExpression
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
            AssertParseExpression(p, n => n is BinaryExpression
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

            AssertParseExpression(p, n => n is BinaryExpression
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

        [Fact]
        public void ComparisonExpressionPrecedence()
        {
            var p = ParserFor(
                @"a <= b <> -c > d"
            );

            // (a <= b) <> (-c > d)
            AssertParseExpression(p, n => n is BinaryExpression
            {
                Operator: BinaryOperator.NotEquals,
                LHS: BinaryExpression
                {
                    Operator: BinaryOperator.LessThanOrEqual,
                    LHS: DeclarationRefExpression { Name: "a" },
                    RHS: DeclarationRefExpression { Name: "b" },
                },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.GreaterThan,
                    LHS: UnaryExpression
                    {
                        Operator: UnaryOperator.Negate,
                        SubExpression: DeclarationRefExpression { Name: "c" },
                    },
                    RHS: DeclarationRefExpression { Name: "d" },
                },
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void CombinedOperatorsExpressionPrecedence()
        {
            var p = ParserFor(
                @"a OR b AND c | d ^ e & f == g > h + i * j"
            );

            // a OR (b AND (c | (d ^ (e & (f == (g > (h + (i * j))))))))
            AssertParseExpression(p, n => n is BinaryExpression
            {
                Operator: BinaryOperator.LogicalOr,
                LHS: DeclarationRefExpression { Name: "a" },
                RHS: BinaryExpression
                {
                    Operator: BinaryOperator.LogicalAnd,
                    LHS: DeclarationRefExpression { Name: "b" },
                    RHS: BinaryExpression
                    {
                        Operator: BinaryOperator.Or,
                        LHS: DeclarationRefExpression { Name: "c" },
                        RHS: BinaryExpression
                        {
                            Operator: BinaryOperator.Xor,
                            LHS: DeclarationRefExpression { Name: "d" },
                            RHS: BinaryExpression
                            {
                                Operator: BinaryOperator.And,
                                LHS: DeclarationRefExpression { Name: "e" },
                                RHS: BinaryExpression
                                {
                                    Operator: BinaryOperator.Equals,
                                    LHS: DeclarationRefExpression { Name: "f" },
                                    RHS: BinaryExpression
                                    {
                                        Operator: BinaryOperator.GreaterThan,
                                        LHS: DeclarationRefExpression { Name: "g" },
                                        RHS: BinaryExpression
                                        {
                                            Operator: BinaryOperator.Add,
                                            LHS: DeclarationRefExpression { Name: "h" },
                                            RHS: BinaryExpression
                                            {
                                                Operator: BinaryOperator.Multiply,
                                                LHS: DeclarationRefExpression { Name: "i" },
                                                RHS: DeclarationRefExpression { Name: "j" },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            });
            True(p.IsAtEOF);
        }
    }
}

namespace ScTools.Tests.ScriptLang
{
    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.Ast.Types;

    using Xunit;
    using static Xunit.Assert;

    public partial class ParserTests
    {
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

            Assert(p.ParseStatement(), n => n is BreakStatement { Label: null });
            Assert(p.ParseStatement(), n => n is ReturnStatement { Label: null });
            Assert(p.ParseStatement(), n => n is GotoStatement { Label: null, TargetLabel: "hello" });
            Assert(p.ParseStatement(), n => n is ContinueStatement { Label: Label { Name: "hello" } });
            Assert(p.ParseStatement(), n => n is BreakStatement { Label: Label { Name: "world" } });
            True(p.IsAtEOF);
        }

        [Fact]
        public void AssignmentStatement()
        {
            var p = ParserFor(
                @"a = b
                  label: c + d = e + f"
            );

            Assert(p.ParseStatement(), n => n is AssignmentStatement
            {
                Label: null,
                CompoundOperator: null,
                LHS: NameExpression { Name: "a" },
                RHS: NameExpression { Name: "b" },
            });
            Assert(p.ParseStatement(), n => n is AssignmentStatement
            {
                Label: Label { Name: "label" },
                CompoundOperator: null,
                LHS: BinaryExpression
                {
                    LHS: NameExpression { Name: "c" },
                    RHS: NameExpression { Name: "d" },
                },
                RHS: BinaryExpression
                {
                    LHS: NameExpression { Name: "e" },
                    RHS: NameExpression { Name: "f" },
                },
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void CompoundAssignmentStatement()
        {
            var p = ParserFor(
                @"a += b
                  label: c + d *= e + f"
            );

            Assert(p.ParseStatement(), n => n is AssignmentStatement
            {
                Label: null,
                CompoundOperator: BinaryOperator.Add,
                LHS: NameExpression { Name: "a" },
                RHS: NameExpression { Name: "b" },
            });
            Assert(p.ParseStatement(), n => n is AssignmentStatement
            {
                Label: Label { Name: "label" },
                CompoundOperator: BinaryOperator.Multiply,
                LHS: BinaryExpression
                {
                    LHS: NameExpression { Name: "c" },
                    RHS: NameExpression { Name: "d" },
                },
                RHS: BinaryExpression
                {
                    LHS: NameExpression { Name: "e" },
                    RHS: NameExpression { Name: "f" },
                },
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void IfStatement()
        {
            var p = ParserFor(
                @"  IF a or \
                       b
                        foo()
                        bar()
                    ENDIF"
            );

            AssertIfStmt(p.ParseStatement(),
                condition => condition is BinaryExpression
                {
                    Operator: BinaryOperator.LogicalOr,
                    LHS: NameExpression { Name: "a" },
                    RHS: NameExpression { Name: "b" },
                },
                then => Collection(then,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)),
                    _1 => AssertInvocation(_1, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))),
                @else => Empty(@else));
            True(p.IsAtEOF);
        }

        [Fact]
        public void IfElseStatement()
        {
            var p = ParserFor(
                @"  IF a
                        foo()
                    ELSE
                        bar()
                    ENDIF"
            );

            AssertIfStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                then => Collection(then,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))),
                @else => Collection(@else,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void IfElifStatement()
        {
            var p = ParserFor(
                @"  IF a
                        foo()
                    ELIF b
                        bar()
                    ENDIF"
            );

            AssertIfStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                then => Collection(then,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))),
                @else => Collection(@else,
                    _0 => AssertIfStmt(_0,
                        condition => condition is NameExpression { Name: "b" },
                        then => Collection(then,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))),
                        @else => Empty(@else))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void IfElifElseStatement()
        {
            var p = ParserFor(
                @"  IF a
                        foo()
                    ELIF b
                        bar()
                    ELSE
                        baz()
                    ENDIF"
            );

            AssertIfStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                then => Collection(then,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))),
                @else => Collection(@else,
                    _0 => AssertIfStmt(_0,
                        condition => condition is NameExpression { Name: "b" },
                        then => Collection(then,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))),
                        @else => Collection(@else,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "baz" }, args => Empty(args))))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void IfWithMultipleElifStatement()
        {
            var p = ParserFor(
                @"  IF a
                        foo()
                    ELIF b
                        bar()
                    ELIF c
                        baz()
                    ELIF d
                        hello()
                    ELSE
                        world()
                    ENDIF"
            );

            AssertIfStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                then => Collection(then,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))),
                @else => Collection(@else,
                    _0 => AssertIfStmt(_0,
                        condition => condition is NameExpression { Name: "b" },
                        then => Collection(then,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))),
                        @else => Collection(@else,
                            _0 => AssertIfStmt(_0,
                                condition => condition is NameExpression { Name: "c" },
                                then => Collection(then,
                                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "baz" }, args => Empty(args))),
                                @else => Collection(@else,
                                    _0 => AssertIfStmt(_0,
                                        condition => condition is NameExpression { Name: "d" },
                                        then => Collection(then,
                                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "hello" }, args => Empty(args))),
                                        @else => Collection(@else,
                                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "world" }, args => Empty(args))))))))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void NestedIfStatement()
        {
            var p = ParserFor(
                @"  IF a
                        foo()
                    ELSE
                        IF b
                            bar()
                        ELSE
                            baz()
                        ENDIF
                    ENDIF"
            );

            AssertIfStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                then => Collection(then,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))),
                @else => Collection(@else,
                    _0 => AssertIfStmt(_0,
                        condition => condition is NameExpression { Name: "b" },
                        then => Collection(then,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))),
                        @else => Collection(@else,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "baz" }, args => Empty(args))))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void EmptyIfStatement()
        {
            var p = ParserFor(
                @"  IF a
                    ENDIF"
            );

            AssertIfStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                then => Empty(then),
                @else => Empty(@else));
            True(p.IsAtEOF);
        }

        [Fact]
        public void EmptyIfElifElseStatement()
        {
            var p = ParserFor(
                @"  IF a
                    ELIF b
                    ELSE
                    ENDIF"
            );

            AssertIfStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                then => Empty(then),
                @else => Collection(@else,
                    _0 => AssertIfStmt(_0,
                        condition => condition is NameExpression { Name: "b" },
                        then => Empty(then),
                        @else => Empty(@else))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void WhileStatement()
        {
            var p = ParserFor(
                @"  WHILE a AND \
                       b
                        foo()
                        bar()
                    ENDWHILE"
            );

            AssertWhileStmt(p.ParseStatement(),
                condition => condition is BinaryExpression
                {
                    Operator: BinaryOperator.LogicalAnd,
                    LHS: NameExpression { Name: "a" },
                    RHS: NameExpression { Name: "b" },
                },
                body => Collection(body,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)),
                    _1 => AssertInvocation(_1, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void NestedWhileStatement()
        {
            var p = ParserFor(
                @"  WHILE a
                        foo()
                        WHILE b
                            bar()
                        ENDWHILE
                    ENDWHILE"
            );

            AssertWhileStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                body => Collection(body,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)),
                    _1 => AssertWhileStmt(_1,
                        condition => condition is NameExpression { Name: "b" },
                        body => Collection(body,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void EmptyWhileStatement()
        {
            var p = ParserFor(
                @"  WHILE a
                    ENDWHILE"
            );

            AssertWhileStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                body => Empty(body));
            True(p.IsAtEOF);
        }

        [Fact]
        public void RepeatStatement()
        {
            var p = ParserFor(
                @"  REPEAT 10 i
                        foo()
                    ENDREPEAT"
            );

            AssertRepeatStmt(p.ParseStatement(),
                limit => limit is IntLiteralExpression { Value: 10 },
                counter => counter is NameExpression { Name: "i" },
                body => Collection(body,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void NestedRepeatStatement()
        {
            var p = ParserFor(
                @"  REPEAT a i
                        foo()
                        REPEAT b k
                            bar()
                        ENDREPEAT
                    ENDREPEAT"
            );

            AssertRepeatStmt(p.ParseStatement(),
                limit => limit is NameExpression { Name: "a" },
                counter => counter is NameExpression { Name: "i" },
                body => Collection(body,
                    _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)),
                    _1 => AssertRepeatStmt(_1,
                        limit => limit is NameExpression { Name: "b" },
                        counter => counter is NameExpression { Name: "k" },
                        body => Collection(body,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void EmptyRepeatStatement()
        {
            var p = ParserFor(
                @"  REPEAT a i
                    ENDREPEAT"
            );

            AssertRepeatStmt(p.ParseStatement(),
                limit => limit is NameExpression { Name: "a" },
                counter => counter is NameExpression { Name: "i" },
                body => Empty(body));
            True(p.IsAtEOF);
        }

        [Fact]
        public void SwitchStatement()
        {
            var p = ParserFor(
                @"  SWITCH a
                    CASE 1
                        foo()
                    CASE 2
                        bar()
                        BREAK
                    DEFAULT
                        baz()
                        BREAK
                    ENDSWITCH"
            );

            AssertSwitchStmt(p.ParseStatement(),
                expr => expr is NameExpression { Name: "a" },
                cases => Collection(cases,
                    _0 => AssertSwitchCase(_0,
                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 1 } },
                        body => Collection(body,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)))),
                    _1 => AssertSwitchCase(_1,
                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 2 } },
                        body => Collection(body,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args)),
                            _1 => True(_1 is BreakStatement))),
                    _2 => AssertSwitchCase(_2,
                        @case => @case is DefaultSwitchCase,
                        body => Collection(body,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "baz" }, args => Empty(args)),
                            _1 => True(_1 is BreakStatement)))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void NestedSwitchStatement()
        {
            var p = ParserFor(
                @"  SWITCH a
                    CASE 1
                        foo()
                    CASE 2
                        SWITCH b
                        CASE 3
                            bar()
                        ENDSWITCH
                    ENDSWITCH"
            );

            AssertSwitchStmt(p.ParseStatement(),
                expr => expr is NameExpression { Name: "a" },
                cases => Collection(cases,
                    _0 => AssertSwitchCase(_0,
                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 1 } },
                        body => Collection(body,
                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)))),
                    _1 => AssertSwitchCase(_1,
                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 2 } },
                        body => Collection(body,
                            _0 => AssertSwitchStmt(_0,
                                expr => expr is NameExpression { Name: "b" },
                                cases => Collection(cases,
                                    _0 => AssertSwitchCase(_0,
                                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 3 } },
                                        body => Collection(body,
                                            _0 => AssertInvocation(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))))))))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void EmptyCasesSwitchStatement()
        {
            var p = ParserFor(
                @"  SWITCH a
                    CASE 1
                    CASE 2
                    DEFAULT
                    CASE 3
                    ENDSWITCH"
            );

            AssertSwitchStmt(p.ParseStatement(),
                expr => expr is NameExpression { Name: "a" },
                cases => Collection(cases,
                    _0 => AssertSwitchCase(_0,
                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 1 } },
                        body => Empty(body)),
                    _1 => AssertSwitchCase(_1,
                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 2 } },
                        body => Empty(body)),
                    _2 => AssertSwitchCase(_2,
                        @case => @case is DefaultSwitchCase,
                        body => Empty(body)),
                    _3 => AssertSwitchCase(_3,
                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 3 } },
                        body => Empty(body))));
            True(p.IsAtEOF);
        }

        [Fact]
        public void EmptySwitchStatement()
        {
            var p = ParserFor(
                @"  SWITCH a
                    ENDSWITCH"
            );

            AssertSwitchStmt(p.ParseStatement(),
                expr => expr is NameExpression { Name: "a" },
                cases => Empty(cases));
            True(p.IsAtEOF);
        }

        [Fact]
        public void VarDeclarationStatement()
        {
            var p = ParserFor(
                @"INT hello
                  label: FLOAT foo, bar"
            );

            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "hello", IsReference: false, Type: NamedType { Name: "INT" }
            });
            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: Label { Name: "label" }, Name: "foo", IsReference: false, Type: NamedType { Name: "FLOAT" }
            });
            Assert(p.ParseStatement(), n => n is VarDeclaration
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

            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "hello", IsReference: false,
                Type: ArrayType { ItemType: NamedType { Name: "INT" }, LengthExpression: IntLiteralExpression { Value: 5 } }
            });
            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: Label { Name: "label" }, Name: "foo", IsReference: false,
                Type: ArrayType { ItemType: NamedType { Name: "FLOAT" }, LengthExpression: IntLiteralExpression { Value: 2 } }
            });
            Assert(p.ParseStatement(), n => n is VarDeclaration
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

            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "hello", IsReference: false,
                Type: ArrayType
                {
                    LengthExpression: IntLiteralExpression { Value: 1 },
                    ItemType: ArrayType
                    {
                        LengthExpression: IntLiteralExpression { Value: 2 },
                        ItemType: NamedType { Name: "INT" },
                    },
                }
            });
            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "foo", IsReference: false,
                Type: ArrayType
                {
                    LengthExpression: IntLiteralExpression { Value: 2 },
                    ItemType: ArrayType
                    {
                        LengthExpression: IntLiteralExpression { Value: 3 },
                        ItemType: ArrayType
                        {
                            LengthExpression: IntLiteralExpression { Value: 5 },
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

            Assert(p.ParseStatement(), n => n is VarDeclaration
            { 
                Label: null, Name: "hello", IsReference: true, Type: NamedType { Name: "INT" }
            });
            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null, Name: "foo", IsReference: true, Type: NamedType { Name: "FLOAT" }
            });
            True(p.IsAtEOF);
        }

        [Fact]
        public void InvocationStatement()
        {
            var p = ParserFor(
                @"foo(1+2, a)
                  bar()
                  baz(0)"
            );

            AssertInvocation(p.ParseStatement(),
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
            AssertInvocation(p.ParseStatement(),
                callee => callee is NameExpression { Name: "bar" },
                args => Empty(args)
                );
            True(p.IsPossibleExpression());
            AssertInvocation(p.ParseStatement(),
                callee => callee is NameExpression { Name: "baz" },
                args => Collection(args,
                    _0 => True(_0 is IntLiteralExpression { Value: 0 })
                    )
                );
            True(p.IsAtEOF);
        }

        [Fact]
        public void ExpressionAsStatementError()
        {
            var p = ParserFor(
                @"1 + 2"
            );

            AssertError(p.ParseStatement(), n => n is ErrorStatement);
            CheckError(ErrorCode.ParserExpressionAsStatement, (1, 1), (1, 5), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void LabelOnEmptyStatement()
        {
            var p = ParserFor(
                @"my_label:
                 "
            );

            Assert(p.ParseStatement(), n => n is EmptyStatement { Label: Label { Name: "my_label" } });
            True(p.IsAtEOF);
        }

        [Fact]
        public void LabelOnNestedEmptyStatement()
        {
            var p = ParserFor(
                @"  WHILE a
                        my_label:
                    ENDWHILE"
            );

            AssertWhileStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                body => Collection(body,
                    _0 => Assert(_0, n => n is EmptyStatement { Label: Label { Name: "my_label" } })));
            True(p.IsAtEOF);
        }

        [Fact]
        public void SequentialLabels()
        {
            var p = ParserFor(
                @"  WHILE a
                        my_label:


                        other_label:

                            bar()
                    ENDWHILE"
            );

            AssertWhileStmt(p.ParseStatement(),
                condition => condition is NameExpression { Name: "a" },
                body => Collection(body,
                    _0 => Assert(_0, n => n is EmptyStatement { Label: Label { Name: "my_label" } }),
                    _1 => Assert(_1, n => n is InvocationExpression { Label: Label { Name: "other_label" }, Callee: NameExpression { Name: "bar" } })));
            True(p.IsAtEOF);
        }
    }
}

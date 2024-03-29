﻿namespace ScTools.Tests.ScriptLang.Parser;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Errors;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;

public partial class ParserTests
{
    public class Statements
    {
        [Fact]
        public void Label()
        {
            var p = ParserFor(
                "my_label:"
            );

            True(p.IsPossibleLabel());
            Assert(p.ParseLabel(), n => n is Label { Name: "my_label" });
            NoErrorsAndIsAtEOF(p);

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

            Assert(p.ParseStatement(), n => n is BreakStatement { Label: null });
            Assert(p.ParseStatement(), n => n is ReturnStatement { Label: null });
            Assert(p.ParseStatement(), n => n is GotoStatement { Label: null, TargetLabel: "hello" });
            Assert(p.ParseStatement(), n => n is ContinueStatement { Label: Label { Name: "hello" } });
            Assert(p.ParseStatement(), n => n is BreakStatement { Label: Label { Name: "world" } });
            NoErrorsAndIsAtEOF(p);
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
            NoErrorsAndIsAtEOF(p);
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
            NoErrorsAndIsAtEOF(p);
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
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)),
                    _1 => AssertInvocationStmt(_1, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))),
                @else => Empty(@else));
            NoErrorsAndIsAtEOF(p);
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
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))),
                @else => Collection(@else,
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))));
            NoErrorsAndIsAtEOF(p);
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
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))),
                @else => Collection(@else,
                    _0 => AssertIfStmt(_0,
                        condition => condition is NameExpression { Name: "b" },
                        then => Collection(then,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))),
                        @else => Empty(@else))));
            NoErrorsAndIsAtEOF(p);
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
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))),
                @else => Collection(@else,
                    _0 => AssertIfStmt(_0,
                        condition => condition is NameExpression { Name: "b" },
                        then => Collection(then,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))),
                        @else => Collection(@else,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "baz" }, args => Empty(args))))));
            NoErrorsAndIsAtEOF(p);
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
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))),
                @else => Collection(@else,
                    _0 => AssertIfStmt(_0,
                        condition => condition is NameExpression { Name: "b" },
                        then => Collection(then,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))),
                        @else => Collection(@else,
                            _0 => AssertIfStmt(_0,
                                condition => condition is NameExpression { Name: "c" },
                                then => Collection(then,
                                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "baz" }, args => Empty(args))),
                                @else => Collection(@else,
                                    _0 => AssertIfStmt(_0,
                                        condition => condition is NameExpression { Name: "d" },
                                        then => Collection(then,
                                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "hello" }, args => Empty(args))),
                                        @else => Collection(@else,
                                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "world" }, args => Empty(args))))))))));
            NoErrorsAndIsAtEOF(p);
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
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))),
                @else => Collection(@else,
                    _0 => AssertIfStmt(_0,
                        condition => condition is NameExpression { Name: "b" },
                        then => Collection(then,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))),
                        @else => Collection(@else,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "baz" }, args => Empty(args))))));
            NoErrorsAndIsAtEOF(p);
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
            NoErrorsAndIsAtEOF(p);
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
            NoErrorsAndIsAtEOF(p);
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
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)),
                    _1 => AssertInvocationStmt(_1, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))));
            NoErrorsAndIsAtEOF(p);
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
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)),
                    _1 => AssertWhileStmt(_1,
                        condition => condition is NameExpression { Name: "b" },
                        body => Collection(body,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))))));
            NoErrorsAndIsAtEOF(p);
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
            NoErrorsAndIsAtEOF(p);
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
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))));
            NoErrorsAndIsAtEOF(p);
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
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)),
                    _1 => AssertRepeatStmt(_1,
                        limit => limit is NameExpression { Name: "b" },
                        counter => counter is NameExpression { Name: "k" },
                        body => Collection(body,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))))));
            NoErrorsAndIsAtEOF(p);
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
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ForStatement()
        {
            var p = ParserFor(
                @"  FOR i = 0 TO 10
                        foo()
                    ENDFOR"
            );

            AssertForStmt(p.ParseStatement(),
                counter => counter is NameExpression { Name: "i" },
                initializer => initializer is IntLiteralExpression { Value: 0 },
                limit => limit is IntLiteralExpression { Value: 10 },
                body => Collection(body,
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args))));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NestedForStatement()
        {
            var p = ParserFor(
                @"  FOR i = 0 TO 10
                        foo()
                        FOR k = 0 TO 10
                            bar()
                        ENDFOR
                    ENDFOR"
            );

            AssertForStmt(p.ParseStatement(),
                counter => counter is NameExpression { Name: "i" },
                initializer => initializer is IntLiteralExpression { Value: 0 },
                limit => limit is IntLiteralExpression { Value: 10 },
                body => Collection(body,
                    _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)),
                    _1 => AssertForStmt(_1,
                        counter => counter is NameExpression { Name: "k" },
                        initializer => initializer is IntLiteralExpression { Value: 0 },
                        limit => limit is IntLiteralExpression { Value: 10 },
                        body => Collection(body,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))))));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void EmptyForStatement()
        {
            var p = ParserFor(
                @"  FOR i = 0 TO 10
                    ENDFOR"
            );

            AssertForStmt(p.ParseStatement(),
                counter => counter is NameExpression { Name: "i" },
                initializer => initializer is IntLiteralExpression { Value: 0 },
                limit => limit is IntLiteralExpression { Value: 10 },
                body => Empty(body));
            NoErrorsAndIsAtEOF(p);
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
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)))),
                    _1 => AssertSwitchCase(_1,
                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 2 } },
                        body => Collection(body,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args)),
                            _1 => True(_1 is BreakStatement))),
                    _2 => AssertSwitchCase(_2,
                        @case => @case is DefaultSwitchCase,
                        body => Collection(body,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "baz" }, args => Empty(args)),
                            _1 => True(_1 is BreakStatement)))));
            NoErrorsAndIsAtEOF(p);
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
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)))),
                    _1 => AssertSwitchCase(_1,
                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 2 } },
                        body => Collection(body,
                            _0 => AssertSwitchStmt(_0,
                                expr => expr is NameExpression { Name: "b" },
                                cases => Collection(cases,
                                    _0 => AssertSwitchCase(_0,
                                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 3 } },
                                        body => Collection(body,
                                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "bar" }, args => Empty(args))))))))));
            NoErrorsAndIsAtEOF(p);
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
            NoErrorsAndIsAtEOF(p);
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
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void SwitchStatementAllowsStatementInSameLineAsCase()
        {
            var p = ParserFor(
                @"  SWITCH a
                    CASE 1   foo()
                    DEFAULT  RETURN 1
                    ENDSWITCH"
            );

            AssertSwitchStmt(p.ParseStatement(),
                expr => expr is NameExpression { Name: "a" },
                cases => Collection(cases,
                    _0 => AssertSwitchCase(_0,
                        @case => @case is ValueSwitchCase { Value: IntLiteralExpression { Value: 1 } },
                        body => Collection(body,
                            _0 => AssertInvocationStmt(_0, callee => callee is NameExpression { Name: "foo" }, args => Empty(args)))),
                    _1 => AssertSwitchCase(_1,
                        @case => @case is DefaultSwitchCase,
                        body => Collection(body,
                            _0 => True(_0 is ReturnStatement { Expression: IntLiteralExpression { Value: 1 } })))));
            NoErrorsAndIsAtEOF(p);
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
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "hello", Declarator: { Name: "hello", IsReference: false, IsArray: false },
                Initializer: null, Kind: VarKind.Local
            });
            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: Label { Name: "label" },
                Type: TypeName { Name: "FLOAT" },
                Name: "foo", Declarator: { Name: "foo", IsReference: false, IsArray: false },
                Initializer: null, Kind: VarKind.Local
            });
            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "FLOAT" },
                Name: "bar", Declarator: { Name: "bar", IsReference: false, IsArray: false },
                Initializer: null, Kind: VarKind.Local
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void VarDeclarationStatementWithInitializer()
        {
            var p = ParserFor(
                @"INT hello = 1
                  label: FLOAT foo = 3.0, bar = 2+1"
            );

            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "hello", Declarator: { Name: "hello", IsReference: false, IsArray: false },
                Initializer: IntLiteralExpression { Value: 1 },
                Kind: VarKind.Local
            });
            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: Label { Name: "label" },
                Type: TypeName { Name: "FLOAT" },
                Name: "foo", Declarator: { Name: "foo", IsReference: false, IsArray: false },
                Initializer: FloatLiteralExpression { Value: 3.0f },
                Kind: VarKind.Local
            });
            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "FLOAT" },
                Name: "bar", Declarator: { Name: "bar", IsReference: false, IsArray: false },
                Initializer: BinaryExpression
                {
                    Operator: BinaryOperator.Add,
                    LHS: IntLiteralExpression { Value: 2 },
                    RHS: IntLiteralExpression { Value: 1 },
                },
                Kind: VarKind.Local
            });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void VarDeclarationStatementWithArrayType()
        {
            var p = ParserFor(
                @"INT hello[5]
                  label: FLOAT foo[2], bar[]"
            );

            AssertArrayVarDeclaration(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "hello",
                Initializer: null, Kind: VarKind.Local
            },
                dim0 => dim0 is IntLiteralExpression { Value: 5 });
            AssertArrayVarDeclaration(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: Label { Name: "label" },
                Type: TypeName { Name: "FLOAT" },
                Name: "foo",
                Initializer: null, Kind: VarKind.Local
            },
                dim0 => dim0 is IntLiteralExpression { Value: 2 });
            AssertArrayVarDeclaration(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "FLOAT" },
                Name: "bar",
                Initializer: null, Kind: VarKind.Local
            },
                dim0 => dim0 is null);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void VarDeclarationStatementWithMultiDimensionalArrayType()
        {
            var p = ParserFor(
                @"INT hello[1][2]
                  FLOAT foo[2][][5+1]"
            );

            AssertArrayVarDeclaration(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "hello",
                Initializer: null, Kind: VarKind.Local
            },
                dim0 => dim0 is IntLiteralExpression { Value: 1 },
                dim1 => dim1 is IntLiteralExpression { Value: 2 });
            AssertArrayVarDeclaration(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "FLOAT" },
                Name: "foo",
                Initializer: null, Kind: VarKind.Local
            },
                dim0 => dim0 is IntLiteralExpression { Value: 2 },
                dim1 => dim1 is null,
                dim2 => dim2 is BinaryExpression
                {
                    Operator: BinaryOperator.Add,
                    LHS: IntLiteralExpression { Value: 5 },
                    RHS: IntLiteralExpression { Value: 1 },
                });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void VarDeclarationStatementIsReference()
        {
            var p = ParserFor(
                @"INT &hello"
            );

            Assert(p.ParseStatement(), n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "hello", Declarator: { Name: "hello", IsReference: true, IsArray: false },
                Initializer: null, Kind: VarKind.Local
            });
            NoErrorsAndIsAtEOF(p); // error is reported during semantic analysis, not parsing
        }

        [Fact]
        public void InvocationStatement()
        {
            var p = ParserFor(
                @"foo(1+2, a)
                  bar()
                  baz(0)"
            );

            AssertInvocationStmt(p.ParseStatement(),
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
            AssertInvocationStmt(p.ParseStatement(),
                callee => callee is NameExpression { Name: "bar" },
                args => Empty(args)
                );
            True(p.IsPossibleExpression());
            AssertInvocationStmt(p.ParseStatement(),
                callee => callee is NameExpression { Name: "baz" },
                args => Collection(args,
                    _0 => True(_0 is IntLiteralExpression { Value: 0 })
                    )
                );
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ExpressionAsStatement()
        {
            var p = ParserFor(
                @"1 + 2"
            );

            Assert(p.ParseStatement(), 
                n => n is ExpressionStatement
                {
                    Expression: BinaryExpression
                    {
                        Operator: BinaryOperator.Add,
                        LHS: IntLiteralExpression { Value: 1 },
                        RHS: IntLiteralExpression { Value: 2 },
                    }
                });
            NoErrorsAndIsAtEOF(p); // error is reported during semantic analysis, not parsing
        }

        [Fact]
        public void LabelOnEmptyStatement()
        {
            var p = ParserFor(
                @"my_label:
                 "
            );

            Assert(p.ParseStatement(), n => n is EmptyStatement { Label: Label { Name: "my_label" } });
            NoErrorsAndIsAtEOF(p);
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
            NoErrorsAndIsAtEOF(p);
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
                    _0 => Assert(_0, n => n is EmptyStatement { Label.Name: "my_label" }),
                    _1 => Assert(_1, n => n is ExpressionStatement
                    {
                        Label.Name: "other_label", 
                        Expression: InvocationExpression
                        {
                            Callee: NameExpression { Name: "bar" }
                        }
                    })));
            NoErrorsAndIsAtEOF(p);
        }
    }
}

namespace ScTools.Tests.ScriptLang
{
    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Statements;

    using Xunit;
    using static Xunit.Assert;

    public partial class ParserTests
    {
        [Fact]
        public void Using()
        {
            var p = ParserFor(
                @"USING 'hello.sc'"
            );

            True(p.IsPossibleUsingDirective());
            Assert(p.ParseUsingDirective(), n => n is UsingDirective { Path: "hello.sc" });
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void UsingWithMissingPath()
        {
            var p = ParserFor(
                @"USING"
            );

            True(p.IsPossibleUsingDirective());
            Assert(p.ParseUsingDirective(), n => n is UsingDirective { Path: ParserNew.MissingUsingPathLexeme });
            // TODO: this SourceRange should probably be the last correct token location
            CheckError(ErrorCode.ParserUnexpectedToken, (0, 0), (0, 0), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void FunctionDeclaration()
        {
            var p = ParserFor(
                @"FUNC BOOL foo(INT a, FLOAT b)
                    INT c
                    c = a * 2
                    RETURN TRUE
                  ENDFUNC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                body => Collection(body,
                    _0 => True(_0 is VarDeclaration_New { Name: "c", Type: TypeName { Name: "INT" } }),
                    _1 => True(_1 is AssignmentStatement
                    {
                        LHS: NameExpression { Name: "c" },
                        RHS: BinaryExpression
                        {
                            Operator: BinaryOperator.Multiply,
                            LHS: NameExpression { Name: "a" },
                            RHS: IntLiteralExpression { Value: 2 },
                        }
                    }),
                    _2 => True(_2 is ReturnStatement { Expression: BoolLiteralExpression { Value: true } })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void EmptyFunctionDeclaration()
        {
            var p = ParserFor(
                @"FUNC BOOL foo(INT a, FLOAT b)
                  ENDFUNC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                body => Empty(body));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void FunctionWithNoParameters()
        {
            var p = ParserFor(
                @"FUNC BOOL foo()
                  ENDFUNC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Empty(@params),
                body => Empty(body));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ProcedureDeclaration()
        {
            var p = ParserFor(
                @"PROC foo(INT a, FLOAT b)
                    INT c
                    c = a * 2
                    RETURN
                  ENDPROC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                body => Collection(body,
                    _0 => True(_0 is VarDeclaration_New { Name: "c", Type: TypeName { Name: "INT" } }),
                    _1 => True(_1 is AssignmentStatement
                    {
                        LHS: NameExpression { Name: "c" },
                        RHS: BinaryExpression
                        {
                            Operator: BinaryOperator.Multiply,
                            LHS: NameExpression { Name: "a" },
                            RHS: IntLiteralExpression { Value: 2 },
                        }
                    }),
                    _2 => True(_2 is ReturnStatement { Expression: null })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void EmptyProcedureDeclaration()
        {
            var p = ParserFor(
                @"PROC foo(INT a, FLOAT b)
                  ENDPROC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                body => Empty(body));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ProcedureWithNoParameters()
        {
            var p = ParserFor(
                @"PROC foo()
                  ENDPROC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Empty(@params),
                body => Empty(body));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void FunctionDeclarationWithMismatchedEndToken()
        {
            var p = ParserFor(
                "FUNC BOOL foo()\nENDPROC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Empty(@params),
                body => Empty(body));
            CheckError(ErrorCode.ParserUnexpectedToken, (2, 1), (2, 7), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void ProcedureDeclarationWithMismatchedEndToken()
        {
            var p = ParserFor(
                "PROC foo()\nENDFUNC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Empty(@params),
                body => Empty(body));
            CheckError(ErrorCode.ParserUnexpectedToken, (2, 1), (2, 7), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void ProcedureDeclarationWithMissingName()
        {
            var p = ParserFor(
                "PROC(INT a, FLOAT b)\nENDPROC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), ParserNew.MissingIdentifierLexeme,
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                body => Empty(body));
            CheckError(ErrorCode.ParserUnexpectedToken, (1, 5), (1, 5), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void FunctionSignatureWithMissingName()
        {
            var p = ParserFor(
                "FUNC BOOL()\nENDFUNC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), ParserNew.MissingIdentifierLexeme,
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Empty(@params),
                body => Empty(body));
            CheckError(ErrorCode.ParserUnexpectedToken, (1, 10), (1, 10), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void FunctionSignatureWithMissingNameAndReturnType()
        {
            var p = ParserFor(
                "FUNC()\nENDFUNC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), ParserNew.MissingIdentifierLexeme,
                retTy => retTy is TypeName { Name: ParserNew.MissingIdentifierLexeme },
                @params => Empty(@params),
                body => Empty(body));
            CheckError(ErrorCode.ParserUnexpectedToken, (1, 5), (1, 5), p.Diagnostics, 2);
            True(p.IsAtEOF);
        }
    }
}

namespace ScTools.Tests.ScriptLang;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;

using Xunit;
using static Xunit.Assert;

public partial class ParserTests
{
    public class Declarations
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
        public void EnumDeclaration()
        {
            var p = ParserFor(
                @"ENUM foo
                    A, B = 1, C
                    D,
                    E = 2 + 1
                    F
                  ENDENUM"
            );

            True(p.IsPossibleEnumDeclaration());
            AssertEnumDeclaration(p.ParseEnumDeclaration(), "foo",
                members => Collection(members,
                    _0 => True(_0 is { Name: "A", Initializer: null }),
                    _1 => True(_1 is { Name: "B", Initializer: IntLiteralExpression { Value: 1 } }),
                    _2 => True(_2 is { Name: "C", Initializer: null }),
                    _3 => True(_3 is { Name: "D", Initializer: null }),
                    _4 => True(_4 is { Name: "E", Initializer: BinaryExpression
                    {
                        Operator: BinaryOperator.Add,
                        LHS: IntLiteralExpression { Value: 2 },
                        RHS: IntLiteralExpression { Value: 1 },
                    } }),
                    _5 => True(_5 is { Name: "F", Initializer: null })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void EmptyEnumDeclaration()
        {
            var p = ParserFor(
                @"ENUM foo
                  ENDENUM"
            );

            True(p.IsPossibleEnumDeclaration());
            AssertEnumDeclaration(p.ParseEnumDeclaration(), "foo",
                members => Empty(members));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void EnumDeclarationWithMissingName()
        {
            var p = ParserFor(
                "ENUM\nENDENUM"
            );

            True(p.IsPossibleEnumDeclaration());
            AssertEnumDeclaration(p.ParseEnumDeclaration(), ParserNew.MissingIdentifierLexeme,
                members => Empty(members));

            CheckError(ErrorCode.ParserUnexpectedToken, (1, 5), (1, 5), p.Diagnostics);

            True(p.IsAtEOF);
        }

        [Fact]
        public void ScriptDeclaration()
        {
            var p = ParserFor(
                @"SCRIPT foo
                    INT c
                    c = 1 * 2
                  ENDSCRIPT"
            );

            True(p.IsPossibleScriptDeclaration());
            AssertScriptDeclaration(p.ParseScriptDeclaration(), "foo",
                @params => Empty(@params),
                body => Collection(body,
                    _0 => True(_0 is VarDeclaration_New { Name: "c", Type: TypeName { Name: "INT" }, Kind: VarKind.Local }),
                    _1 => True(_1 is AssignmentStatement
                    {
                        LHS: NameExpression { Name: "c" },
                        RHS: BinaryExpression
                        {
                            Operator: BinaryOperator.Multiply,
                            LHS: IntLiteralExpression { Value: 1 },
                            RHS: IntLiteralExpression { Value: 2 },
                        }
                    })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void EmptyScriptDeclaration()
        {
            var p = ParserFor(
                @"SCRIPT foo
                  ENDSCRIPT"
            );

            True(p.IsPossibleScriptDeclaration());
            AssertScriptDeclaration(p.ParseScriptDeclaration(), "foo",
                @params => Empty(@params),
                body => Empty(body));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ScriptDeclarationWithParameterList()
        {
            var p = ParserFor(
                @"SCRIPT foo(INT a, FLOAT b)
                  ENDSCRIPT"
            );

            True(p.IsPossibleScriptDeclaration());
            AssertScriptDeclaration(p.ParseScriptDeclaration(), "foo",
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.ScriptParameter }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.ScriptParameter })),
                body => Empty(body));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ScriptDeclarationWithEmptyParameterList()
        {
            var p = ParserFor(
                @"SCRIPT foo()
                  ENDSCRIPT"
            );

            True(p.IsPossibleScriptDeclaration());
            AssertScriptDeclaration(p.ParseScriptDeclaration(), "foo",
                @params => Empty(@params),
                body => Empty(body));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ScriptDeclarationWithMissingName()
        {
            var p = ParserFor(
                "SCRIPT\nENDSCRIPT"
            );

            True(p.IsPossibleScriptDeclaration());
            AssertScriptDeclaration(p.ParseScriptDeclaration(), ParserNew.MissingIdentifierLexeme,
                @params => Empty(@params),
                body => Empty(body));
            CheckError(ErrorCode.ParserUnexpectedToken, (1, 7), (1, 7), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void ScriptDeclarationWithParametersWithInitializers()
        {
            var p = ParserFor(
                @"SCRIPT foo(INT a = 1, FLOAT b = 3.0)
                  ENDSCRIPT"
            );

            True(p.IsPossibleScriptDeclaration());
            AssertScriptDeclaration(p.ParseScriptDeclaration(), "foo",
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.ScriptParameter, Initializer: IntLiteralExpression{ Value: 1 } }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.ScriptParameter, Initializer: FloatLiteralExpression { Value: 3.0f } })),
                body => Empty(body));

            CheckError(ErrorCode.ParserVarInitializerNotAllowed, (1, 18), (1, 20), p.Diagnostics);
            CheckError(ErrorCode.ParserVarInitializerNotAllowed, (1, 31), (1, 35), p.Diagnostics);

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
                    _0 => True(_0 is VarDeclaration_New { Name: "c", Type: TypeName { Name: "INT" }, Kind: VarKind.Local }),
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
        public void FunctionDeclarationWithParametersWithInitializers()
        {
            var p = ParserFor(
                @"FUNC BOOL foo(INT a = 1, FLOAT b = 3.0)
                ENDFUNC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter, Initializer: IntLiteralExpression { Value: 1 } }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter, Initializer: FloatLiteralExpression { Value: 3.0f } })),
                body => Empty(body));

            CheckError(ErrorCode.ParserVarInitializerNotAllowed, (1, 21), (1, 23), p.Diagnostics);
            CheckError(ErrorCode.ParserVarInitializerNotAllowed, (1, 34), (1, 38), p.Diagnostics);

            True(p.IsAtEOF);
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
                    _0 => True(_0 is VarDeclaration_New { Name: "c", Type: TypeName { Name: "INT" }, Kind: VarKind.Local }),
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
        public void ProcedureDeclarationWithParametersWithInitializers()
        {
            var p = ParserFor(
                @"PROC foo(INT a = 1, FLOAT b = 3.0)
                ENDPROC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter, Initializer: IntLiteralExpression { Value: 1 } }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter, Initializer: FloatLiteralExpression { Value: 3.0f } })),
                body => Empty(body));

            CheckError(ErrorCode.ParserVarInitializerNotAllowed, (1, 16), (1, 18), p.Diagnostics);
            CheckError(ErrorCode.ParserVarInitializerNotAllowed, (1, 29), (1, 33), p.Diagnostics);

            True(p.IsAtEOF);
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

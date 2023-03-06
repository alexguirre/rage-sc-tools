namespace ScTools.Tests.ScriptLang.Parser;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Statements;

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
            Assert(p.ParseUsingDirective(), n => n is UsingDirective { Path: Parser.MissingUsingPathLexeme });
            // TODO: this SourceRange should probably be the last correct token location
            CheckError(ErrorCode.ParserUnexpectedToken, (0, 0), (0, 0), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void EnumDeclaration()
        {
            var p = ParserFor(
                @"ENUM foo
                    A, B = 1, C,
                    D,
                    E = 2 + 1,
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
            AssertEnumDeclaration(p.ParseEnumDeclaration(), Parser.MissingIdentifierLexeme,
                members => Empty(members));

            CheckError(ErrorCode.ParserUnexpectedToken, (1, 5), (1, 5), p.Diagnostics);

            True(p.IsAtEOF);
        }

        [Fact]
        public void EnumCommasOnNextLineAreAllowed()
        {
            var p = ParserFor(
                @"ENUM foo
                    A = 1
                    ,B = 2
                    ,C  = 4
                ENDENUM");

            True(p.IsPossibleEnumDeclaration());
            AssertEnumDeclaration(p.ParseEnumDeclaration(), "foo",
                members => Collection(members,
                    _0 => True(_0 is { Name: "A", Initializer: IntLiteralExpression { Value: 1 } }),
                    _1 => True(_1 is { Name: "B", Initializer: IntLiteralExpression { Value: 2 } }),
                    _2 => True(_2 is { Name: "C", Initializer: IntLiteralExpression { Value: 4 } })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void EnumTrailingCommaIsAllowed()
        {
            // R*'s compiler doesn't seem to allow trailing commas, but it's annoying to implement in our parser
            var p = ParserFor(
                @"ENUM foo
                    A,
                    B,
                    C,
                ENDENUM");
            
            True(p.IsPossibleEnumDeclaration());
            AssertEnumDeclaration(p.ParseEnumDeclaration(), "foo",
                members => Collection(members,
                    _0 => True(_0 is { Name: "A", Initializer: null }),
                    _1 => True(_1 is { Name: "B", Initializer: null }),
                    _2 => True(_2 is { Name: "C", Initializer: null })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void StructDeclaration()
        {
            var p = ParserFor(
                @"STRUCT foo
                    INT a, b = 1, c
                    FLOAT d[5]
                    INT e = 2 + 1
                    BOOL f
                  ENDSTRUCT"
            );

            True(p.IsPossibleStructDeclaration());
            AssertStructDeclaration(p.ParseStructDeclaration(), "foo",
                fields => Collection(fields,
                    _0 => True(_0 is
                    {
                        Name: "a", Declarator: { Name: "a", IsReference: false, IsArray: false },
                        Type: TypeName { Name: "INT" },
                        Initializer: null,
                        Kind: VarKind.Field
                    }),
                    _1 => True(_1 is
                    {
                        Name: "b", Declarator: { Name: "b", IsReference: false, IsArray: false },
                        Type: TypeName { Name: "INT" },
                        Initializer: IntLiteralExpression { Value: 1 },
                        Kind: VarKind.Field
                    }),
                    _2 => True(_2 is
                    {
                        Name: "c", Declarator: { Name: "c", IsReference: false, IsArray: false },
                        Type: TypeName { Name: "INT" },
                        Initializer: null,
                        Kind: VarKind.Field
                    }),
                    _3 => True(_3 is
                    {
                        Name: "d", Declarator: { Name: "d", IsReference: false, IsArray: true, Rank: 1, Lengths: [IntLiteralExpression { Value: 5 }] },
                        Type: TypeName { Name: "FLOAT" },
                        Initializer: null,
                        Kind: VarKind.Field
                    }),
                    _4 => True(_4 is
                    {
                        Name: "e", Declarator: { Name: "e", IsReference: false, IsArray: false },
                        Type: TypeName { Name: "INT" },
                        Initializer: BinaryExpression
                        {
                            Operator: BinaryOperator.Add,
                            LHS: IntLiteralExpression { Value: 2 },
                            RHS: IntLiteralExpression { Value: 1 },
                        },
                        Kind: VarKind.Field
                    }),
                    _5 => True(_5 is
                    {
                        Name: "f", Declarator: { Name: "f", IsReference: false, IsArray: false },
                        Type: TypeName { Name: "BOOL" },
                        Initializer: null,
                        Kind: VarKind.Field
                    })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void EmptyStructDeclaration()
        {
            var p = ParserFor(
                @"STRUCT foo
                  ENDSTRUCT"
            );

            True(p.IsPossibleStructDeclaration());
            AssertStructDeclaration(p.ParseStructDeclaration(), "foo",
                fields => Empty(fields));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void StructDeclarationWithMissingName()
        {
            var p = ParserFor(
                "STRUCT\nENDSTRUCT"
            );

            True(p.IsPossibleStructDeclaration());
            AssertStructDeclaration(p.ParseStructDeclaration(), Parser.MissingIdentifierLexeme,
                fields => Empty(fields));

            CheckError(ErrorCode.ParserUnexpectedToken, (1, 7), (1, 7), p.Diagnostics);

            True(p.IsAtEOF);
        }

        [Fact]
        public void GlobalBlockDeclaration()
        {
            var p = ParserFor(
                @"GLOBAL foo 1
                    INT a, b = 1, c
                    FLOAT d[5]
                    INT e = 2 + 1
                    BOOL f
                  ENDGLOBAL"
            );

            True(p.IsPossibleGlobalBlockDeclaration());
            AssertGlobalBlockDeclaration(p.ParseGlobalBlockDeclaration(), "foo", 1,
                vars => Collection(vars,
                    _0 => True(_0 is
                    {
                        Name: "a", Declarator: { Name: "a", IsReference: false, IsArray: false },
                        Type: TypeName { Name: "INT" },
                        Initializer: null,
                        Kind: VarKind.Global
                    }),
                    _1 => True(_1 is
                    {
                        Name: "b", Declarator: { Name: "b", IsReference: false, IsArray: false },
                        Type: TypeName { Name: "INT" },
                        Initializer: IntLiteralExpression { Value: 1 },
                        Kind: VarKind.Global
                    }),
                    _2 => True(_2 is
                    {
                        Name: "c", Declarator: { Name: "c", IsReference: false, IsArray: false },
                        Type: TypeName { Name: "INT" },
                        Initializer: null,
                        Kind: VarKind.Global
                    }),
                    _3 => True(_3 is
                    {
                        Name: "d", Declarator: { Name: "d", IsReference: false, IsArray: true, Rank: 1, Lengths: [IntLiteralExpression { Value: 5 }]  },
                        Type: TypeName { Name: "FLOAT" },
                        Initializer: null,
                        Kind: VarKind.Global
                    }),
                    _4 => True(_4 is
                    {
                        Name: "e", Declarator: { Name: "e", IsReference: false, IsArray: false },
                        Type: TypeName { Name: "INT" },
                        Initializer: BinaryExpression
                        {
                            Operator: BinaryOperator.Add,
                            LHS: IntLiteralExpression { Value: 2 },
                            RHS: IntLiteralExpression { Value: 1 },
                        },
                        Kind: VarKind.Global
                    }),
                    _5 => True(_5 is
                    {
                        Name: "f", Declarator: { Name: "f", IsReference: false, IsArray: false },
                        Type: TypeName { Name: "BOOL" },
                        Initializer: null,
                        Kind: VarKind.Global
                    })));
            NoErrorsAndIsAtEOF(p);
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
                    _0 => True(_0 is VarDeclaration { Name: "c", Type: TypeName { Name: "INT" }, Kind: VarKind.Local }),
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
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.ScriptParameter }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.ScriptParameter })),
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
            AssertScriptDeclaration(p.ParseScriptDeclaration(), Parser.MissingIdentifierLexeme,
                @params => Empty(@params),
                body => Empty(body));
            CheckError(ErrorCode.ParserUnexpectedToken, (1, 7), (1, 7), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void ScriptDeclarationWithOptionalParameters()
        {
            var p = ParserFor(
                @"SCRIPT foo(INT a = 1, FLOAT b = 3.0)
                  ENDSCRIPT"
            );

            True(p.IsPossibleScriptDeclaration());
            AssertScriptDeclaration(p.ParseScriptDeclaration(), "foo",
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.ScriptParameter, Initializer: IntLiteralExpression{ Value: 1 } }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.ScriptParameter, Initializer: FloatLiteralExpression { Value: 3.0f } })),
                body => Empty(body));
            NoErrorsAndIsAtEOF(p); // no error during parsing, errors during semantic phase
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
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                body => Collection(body,
                    _0 => True(_0 is VarDeclaration { Name: "c", Type: TypeName { Name: "INT" }, Kind: VarKind.Local }),
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
                    _2 => True(_2 is ReturnStatement { Expression: BoolLiteralExpression { Value: true } })),
                isDebugOnly: false);
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
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                body => Empty(body),
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void FunctionDeclarationWithOptionalParameters()
        {
            var p = ParserFor(
                @"FUNC BOOL foo(INT a = 1, FLOAT b = 3.0)
                ENDFUNC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter, Initializer: IntLiteralExpression { Value: 1 } }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter, Initializer: FloatLiteralExpression { Value: 3.0f } })),
                body => Empty(body),
                isDebugOnly: false);
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
                body => Empty(body),
                isDebugOnly: false);
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
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                body => Collection(body,
                    _0 => True(_0 is VarDeclaration { Name: "c", Type: TypeName { Name: "INT" }, Kind: VarKind.Local }),
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
                    _2 => True(_2 is ReturnStatement { Expression: null })),
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ProcedureDeclarationWithOptionalParameters()
        {
            var p = ParserFor(
                @"PROC foo(INT a = 1, FLOAT b = 3.0)
                ENDPROC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter, Initializer: IntLiteralExpression { Value: 1 } }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter, Initializer: FloatLiteralExpression { Value: 3.0f } })),
                body => Empty(body),
                isDebugOnly: false);
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
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                body => Empty(body),
                isDebugOnly: false);
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
                body => Empty(body),
                isDebugOnly: false);
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
                body => Empty(body),
                isDebugOnly: false);
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
                body => Empty(body),
                isDebugOnly: false);
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
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), Parser.MissingIdentifierLexeme,
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                body => Empty(body),
                isDebugOnly: false);
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
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), Parser.MissingIdentifierLexeme,
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Empty(@params),
                body => Empty(body),
                isDebugOnly: false);
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
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), Parser.MissingIdentifierLexeme,
                retTy => retTy is TypeName { Name: Parser.MissingIdentifierLexeme },
                @params => Empty(@params),
                body => Empty(body),
                isDebugOnly: false);
            CheckError(ErrorCode.ParserUnexpectedToken, (1, 5), (1, 5), p.Diagnostics, 2);
            True(p.IsAtEOF);
        }

        [Fact]
        public void FunctionCanBeDebugOnly()
        {
            var p = ParserFor(
                @"DEBUGONLY FUNC BOOL foo()
                ENDFUNC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Empty(@params),
                body => Empty(body),
                isDebugOnly: true);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ProcedureCanBeDebugOnly()
        {
            var p = ParserFor(
                @"DEBUGONLY PROC foo()
                ENDPROC"
            );

            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Empty(@params),
                body => Empty(body),
                isDebugOnly: true);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void FunctionTypeDefDeclaration()
        {
            var p = ParserFor(
                @"TYPEDEF FUNC BOOL foo(INT a, FLOAT b)"
            );

            True(p.IsPossibleFunctionTypeDefDeclaration());
            AssertFunctionTypeDefDeclaration(p.ParseFunctionTypeDefDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void FunctionTypeDefDeclarationWithOptionalParameters()
        {
            var p = ParserFor(
                @"TYPEDEF FUNC BOOL foo(INT a = 1, FLOAT b = 3.0)"
            );

            True(p.IsPossibleFunctionTypeDefDeclaration());
            AssertFunctionTypeDefDeclaration(p.ParseFunctionTypeDefDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter, Initializer: IntLiteralExpression { Value: 1 } }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter, Initializer: FloatLiteralExpression { Value: 3.0f } })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void FunctionTypeDefWithNoParameters()
        {
            var p = ParserFor(
                @"TYPEDEF FUNC BOOL foo()"
            );

            True(p.IsPossibleFunctionTypeDefDeclaration());
            AssertFunctionTypeDefDeclaration(p.ParseFunctionTypeDefDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Empty(@params));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ProcedureTypeDefDeclaration()
        {
            var p = ParserFor(
                @"TYPEDEF PROC foo(INT a, FLOAT b)"
            );

            True(p.IsPossibleFunctionTypeDefDeclaration());
            AssertFunctionTypeDefDeclaration(p.ParseFunctionTypeDefDeclaration(), "foo",
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ProcedureTypeDefDeclarationWithOptionalParameters()
        {
            var p = ParserFor(
                @"TYPEDEF PROC foo(INT a = 1, FLOAT b = 3.0)"
            );

            True(p.IsPossibleFunctionTypeDefDeclaration());
            AssertFunctionTypeDefDeclaration(p.ParseFunctionTypeDefDeclaration(), "foo",
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter, Initializer: IntLiteralExpression { Value: 1 } }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter, Initializer: FloatLiteralExpression { Value: 3.0f } })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ProcedureTypeDefWithNoParameters()
        {
            var p = ParserFor(
                @"TYPEDEF PROC foo()"
            );

            True(p.IsPossibleFunctionTypeDefDeclaration());
            AssertFunctionTypeDefDeclaration(p.ParseFunctionTypeDefDeclaration(), "foo",
                retTy => retTy is null,
                @params => Empty(@params));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeFunctionDeclaration()
        {
            var p = ParserFor(
                @"NATIVE FUNC BOOL foo(INT a, FLOAT b)"
            );

            True(p.IsPossibleNativeFunctionDeclaration());
            AssertNativeFunctionDeclaration(p.ParseNativeFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                id => id is null,
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeFunctionDeclarationWithOptionalParameters()
        {
            var p = ParserFor(
                @"NATIVE FUNC BOOL foo(INT a = 1, FLOAT b = 3.0)"
            );

            True(p.IsPossibleNativeFunctionDeclaration());
            AssertNativeFunctionDeclaration(p.ParseNativeFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter, Initializer: IntLiteralExpression { Value: 1 } }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter, Initializer: FloatLiteralExpression { Value: 3.0f } })),
                id => id is null,
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeFunctionWithNoParameters()
        {
            var p = ParserFor(
                @"NATIVE FUNC BOOL foo()"
            );

            True(p.IsPossibleNativeFunctionDeclaration());
            AssertNativeFunctionDeclaration(p.ParseNativeFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Empty(@params),
                id => id is null,
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeProcedureDeclaration()
        {
            var p = ParserFor(
                @"NATIVE PROC foo(INT a, FLOAT b)"
            );

            True(p.IsPossibleNativeFunctionDeclaration());
            AssertNativeFunctionDeclaration(p.ParseNativeFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })),
                id => id is null,
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeProcedureDeclarationWithOptionalParameters()
        {
            var p = ParserFor(
                @"NATIVE PROC foo(INT a = 1, FLOAT b = 3.0)"
            );

            True(p.IsPossibleNativeFunctionDeclaration());
            AssertNativeFunctionDeclaration(p.ParseNativeFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter, Initializer: IntLiteralExpression { Value: 1 } }),
                    _1 => True(_1 is VarDeclaration { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter, Initializer: FloatLiteralExpression { Value: 3.0f } })),
                id => id is null,
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeProcedureWithNoParameters()
        {
            var p = ParserFor(
                @"NATIVE PROC foo()"
            );

            True(p.IsPossibleNativeFunctionDeclaration());
            AssertNativeFunctionDeclaration(p.ParseNativeFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Empty(@params),
                id => id is null,
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeFunctionCanHaveId()
        {
            var p = ParserFor(
                @"NATIVE FUNC INT foo() = ""my_id"""
            );

            True(p.IsPossibleNativeFunctionDeclaration());
            AssertNativeFunctionDeclaration(p.ParseNativeFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "INT" },
                @params => Empty(@params),
                id => id is StringLiteralExpression { Value: "my_id" },
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeProcedureCanHaveId()
        {
            var p = ParserFor(
                @"NATIVE PROC foo() = ""my_id"""
            );

            True(p.IsPossibleNativeFunctionDeclaration());
            AssertNativeFunctionDeclaration(p.ParseNativeFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Empty(@params),
                id => id is StringLiteralExpression { Value: "my_id" },
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeFunctionCanBeDebugOnly()
        {
            var p = ParserFor(
                @"NATIVE DEBUGONLY FUNC INT foo()"
            );

            True(p.IsPossibleNativeFunctionDeclaration());
            AssertNativeFunctionDeclaration(p.ParseNativeFunctionDeclaration(), "foo",
                retTy => retTy is TypeName { Name: "INT" },
                @params => Empty(@params),
                id => id is null,
                isDebugOnly: true);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeProcedureCanBeDebugOnly()
        {
            var p = ParserFor(
                @"NATIVE DEBUGONLY PROC foo()"
            );

            True(p.IsPossibleNativeFunctionDeclaration());
            AssertNativeFunctionDeclaration(p.ParseNativeFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Empty(@params),
                id => id is null,
                isDebugOnly: true);
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void NativeType()
        {
            var p = ParserFor(
                @"NATIVE FOO_ID"
            );

            True(p.IsPossibleNativeTypeDeclaration());
            AssertNativeTypeDeclaration(p.ParseNativeTypeDeclaration(), "FOO_ID");
            NoErrorsAndIsAtEOF(p);
        }

        [Theory]
        [InlineData(@"
            PROC foo(INT a,
                     INT b,
                     INT c)
            ENDPROC")]
        [InlineData(@"
            PROC foo(
                     INT a,
                     INT b,
                     INT c)
            ENDPROC")]
        [InlineData(@"
            PROC foo(INT a,
                     INT b,
                     INT c
                    )
            ENDPROC")]
        [InlineData(@"
            PROC foo(
                     INT a,
                     INT b,
                     INT c
                    )
            ENDPROC")]
        public void ParametersOnMultipleLines(string src)
        {
            var p = ParserFor(src);
            p.AcceptEOS();
            True(p.IsPossibleFunctionDeclaration());
            AssertFunctionDeclaration(p.ParseFunctionDeclaration(), "foo",
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is { Name: "b", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _2 => True(_2 is { Name: "c", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter })),
                body => Empty(body),
                isDebugOnly: false);
            NoErrorsAndIsAtEOF(p);
        }
    }
}

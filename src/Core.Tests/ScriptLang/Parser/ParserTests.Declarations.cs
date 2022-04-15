namespace ScTools.Tests.ScriptLang
{
    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;

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
        public void FunctionSignature()
        {
            var p = ParserFor(
                @"FUNC BOOL foo(INT a, FLOAT b)"
            );

            True(p.IsPossibleFunctionSignature());
            AssertFunctionSignature(p.ParseFunctionSignature(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void FunctionSignatureWithNoParameters()
        {
            var p = ParserFor(
                @"FUNC BOOL foo()"
            );

            True(p.IsPossibleFunctionSignature());
            AssertFunctionSignature(p.ParseFunctionSignature(), "foo",
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Empty(@params));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ProcedureSignature()
        {
            var p = ParserFor(
                @"PROC foo(INT a, FLOAT b)"
            );

            True(p.IsPossibleFunctionSignature());
            AssertFunctionSignature(p.ParseFunctionSignature(), "foo",
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ProcedureSignatureWithNoParameters()
        {
            var p = ParserFor(
                @"PROC foo()"
            );

            True(p.IsPossibleFunctionSignature());
            AssertFunctionSignature(p.ParseFunctionSignature(), "foo",
                retTy => retTy is null,
                @params => Empty(@params));
            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ProcedureSignatureWithMissingName()
        {
            var p = ParserFor(
                @"PROC(INT a, FLOAT b)"
            );

            True(p.IsPossibleFunctionSignature());
            AssertFunctionSignature(p.ParseFunctionSignature(), ParserNew.MissingIdentifierLexeme,
                retTy => retTy is null,
                @params => Collection(@params,
                    _0 => True(_0 is VarDeclaration_New { Name: "a", Type: TypeName { Name: "INT" }, Kind: VarKind.Parameter }),
                    _1 => True(_1 is VarDeclaration_New { Name: "b", Type: TypeName { Name: "FLOAT" }, Kind: VarKind.Parameter })));
            CheckError(ErrorCode.ParserUnexpectedToken, (1, 5), (1, 5), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void FunctionSignatureWithMissingName()
        {
            var p = ParserFor(
                @"FUNC BOOL()"
            );

            True(p.IsPossibleFunctionSignature());
            AssertFunctionSignature(p.ParseFunctionSignature(), ParserNew.MissingIdentifierLexeme,
                retTy => retTy is TypeName { Name: "BOOL" },
                @params => Empty(@params));
            CheckError(ErrorCode.ParserUnexpectedToken, (1, 10), (1, 10), p.Diagnostics);
            True(p.IsAtEOF);
        }

        [Fact]
        public void FunctionSignatureWithMissingNameAndReturnType()
        {
            var p = ParserFor(
                @"FUNC()"
            );

            True(p.IsPossibleFunctionSignature());
            AssertFunctionSignature(p.ParseFunctionSignature(), ParserNew.MissingIdentifierLexeme,
                retTy => retTy is TypeName { Name: ParserNew.MissingIdentifierLexeme },
                @params => Empty(@params));
            CheckError(ErrorCode.ParserUnexpectedToken, (1, 5), (1, 5), p.Diagnostics, 2);
            True(p.IsAtEOF);
        }
    }
}

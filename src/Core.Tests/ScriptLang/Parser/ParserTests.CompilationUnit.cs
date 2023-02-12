namespace ScTools.Tests.ScriptLang.Parser;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Errors;
using ScTools.ScriptLang.Ast.Expressions;

public partial class ParserTests
{
    public class CompilationUnit
    {
        [Fact]
        public void BasicCompilationUnit()
        {
            var p = ParserFor(
                @"USING 'hello.sch'
                  USING 'world.sch'

                  FUNC INT foo()
                  ENDFUNC

                  SCRIPT bar
                  ENDSCRIPT"
            );

            var u = p.ParseCompilationUnit();
            Assert(u.Usings[0], n => n is UsingDirective { Path: "hello.sch" });
            Assert(u.Usings[1], n => n is UsingDirective { Path: "world.sch" });

            AssertFunctionDeclaration(u.Declarations[0], "foo",
                retTy => retTy is TypeName { Name: "INT" },
                @params => Empty(@params),
                body => Empty(body));

            AssertScriptDeclaration(u.Declarations[1], "bar",
                @params => Empty(@params),
                body => Empty(body));

            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void CompilationUnitCanStartWithEmptyLines()
        {
            var p = ParserFor(
                @"
                  /*empty*/

                  USING 'hello.sch'

                  SCRIPT bar
                  ENDSCRIPT"
            );

            var u = p.ParseCompilationUnit();
            Assert(u.Usings[0], n => n is UsingDirective { Path: "hello.sch" });

            AssertScriptDeclaration(u.Declarations[0], "bar",
                @params => Empty(@params),
                body => Empty(body));

            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void EmptyCompilationUnit()
        {
            var p = ParserFor(
                @""
            );

            var u = p.ParseCompilationUnit();
            Empty(u.Usings);
            Empty(u.Declarations);

            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void StaticVars()
        {
            var p = ParserFor(
                @"INT a, b"
            );

            var u = p.ParseCompilationUnit();
            Assert(u.Declarations[0], n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "a", Declarator: VarDeclarator { Name: "a" },
                Initializer: null, Kind: VarKind.Static
            });
            Assert(u.Declarations[1], n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "b", Declarator: VarDeclarator { Name: "b" },
                Initializer: null, Kind: VarKind.Static
            });

            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void StaticVarsWithInitializers()
        {
            var p = ParserFor(
                @"INT a = 1, b = 2
                  BOOL c = TRUE"
            );

            var u = p.ParseCompilationUnit();
            Assert(u.Declarations[0], n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "a", Declarator: VarDeclarator { Name: "a" },
                Initializer: IntLiteralExpression { Value: 1 },
                Kind: VarKind.Static
            });
            Assert(u.Declarations[1], n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "b", Declarator: VarDeclarator { Name: "b" },
                Initializer: IntLiteralExpression { Value: 2 },
                Kind: VarKind.Static
            });
            Assert(u.Declarations[2], n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "BOOL" },
                Name: "c", Declarator: VarDeclarator { Name: "c" },
                Initializer: BoolLiteralExpression { Value: true },
                Kind: VarKind.Static
            });

            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void Constants()
        {
            var p = ParserFor(
                @"CONST_INT a 1
                  CONST_INT b 2"
            );

            var u = p.ParseCompilationUnit();
            Assert(u.Declarations[0], n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "a", Declarator: VarDeclarator { Name: "a" },
                Initializer: IntLiteralExpression { Value: 1 },
                Kind: VarKind.Constant
            });
            Assert(u.Declarations[1], n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "b", Declarator: VarDeclarator { Name: "b" },
                Initializer: IntLiteralExpression { Value: 2 },
                Kind: VarKind.Constant
            });

            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void ConstantsAndStaticVars()
        {
            var p = ParserFor(
                @"CONST_INT a 1
                  INT b = 2"
            );

            var u = p.ParseCompilationUnit();
            Assert(u.Declarations[0], n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "a", Declarator: VarDeclarator { Name: "a" },
                Initializer: IntLiteralExpression { Value: 1 },
                Kind: VarKind.Constant
            });
            Assert(u.Declarations[1], n => n is VarDeclaration
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "b", Declarator: VarDeclarator { Name: "b" },
                Initializer: IntLiteralExpression { Value: 2 },
                Kind: VarKind.Static
            });

            NoErrorsAndIsAtEOF(p);
        }

        [Fact]
        public void UsingAfterDeclaration()
        {
            var p = ParserFor(
                @"USING 'hello.sch'

                  SCRIPT bar
                  ENDSCRIPT

                  USING 'world.sch'"
            );

            var u = p.ParseCompilationUnit();
            Assert(u.Usings[0], n => n is UsingDirective { Path: "hello.sch" });
            Assert(u.Usings[1], n => n is UsingDirective { Path: "world.sch" });

            AssertScriptDeclaration(u.Declarations[0], "bar",
                @params => Empty(@params),
                body => Empty(body));

            CheckError(ErrorCode.ParserUsingAfterDeclaration, (6, 19), (6, 35), p.Diagnostics);

            True(p.IsAtEOF);
        }

        [Fact]
        public void UnknownDeclaration()
        {
            var p = ParserFor(
                @"something /"
            );

            var u = p.ParseCompilationUnit();

            True(u.Declarations[0] is ErrorDeclaration);

            CheckError(ErrorCode.ParserUnknownDeclaration, (1, 1), (1, 9), p.Diagnostics);

            True(p.IsAtEOF);
        }
    }
}

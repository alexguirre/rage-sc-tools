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
        public void StaticVars()
        {
            var p = ParserFor(
                @"INT a, b"
            );

            var u = p.ParseCompilationUnit();
            Assert(u.Declarations[0], n => n is VarDeclaration_New
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "a", Declarator: VarDeclarator { Name: "a" },
                Initializer: null, Kind: VarKind.Static
            });
            Assert(u.Declarations[1], n => n is VarDeclaration_New
            {
                Label: null,
                Type: TypeName { Name: "INT" },
                Name: "b", Declarator: VarDeclarator { Name: "b" },
                Initializer: null, Kind: VarKind.Static
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
    }
}

namespace ScTools.Tests.ScriptLang.Semantics;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Semantics;

public class UsingsTests : SemanticsTestsBase
{
    [Fact]
    public void Using()
    {
        const string LibA = @"
        FUNC INT GET_A()
            RETURN 1
        ENDFUNC
        ";
        const string LibB = @"
        FUNC INT GET_B()
            RETURN 2
        ENDFUNC
        ";
        const string Script = @"
        USING 'a.sch'
        USING 'b.sch'

        SCRIPT my_script
            INT a = GET_A()
            INT b = GET_B()
        ENDSCRIPT
        ";

        var a = AnalyzeAndAst(LibA).Ast;
        var b = AnalyzeAndAst(LibB).Ast;

        var usingResolver = new Mock<IUsingResolver>();
        usingResolver.Setup(r => r.ResolveUsingAsync("a.sch").Result)
                     .Returns(new UsingResolveResult(UsingResolveStatus.Valid, a));
        usingResolver.Setup(r => r.ResolveUsingAsync("b.sch").Result)
                     .Returns(new UsingResolveResult(UsingResolveStatus.Valid, b));

        var (s, ast) = AnalyzeAndAst(Script, usingResolver.Object);
        False(s.Diagnostics.HasErrors);
        Same(a, ast.FindNthNodeOfType<UsingDirective>(1).Semantics.ImportedCompilationUnit);
        Same(b, ast.FindNthNodeOfType<UsingDirective>(2).Semantics.ImportedCompilationUnit);
        Same(a.Declarations[0], ast.FindNthNodeOfType<NameExpression>(1).Semantics.Symbol);
        Same(b.Declarations[0], ast.FindNthNodeOfType<NameExpression>(2).Semantics.Symbol);

        usingResolver.Verify(r => r.ResolveUsingAsync("a.sch").Result, Times.Once);
        usingResolver.Verify(r => r.ResolveUsingAsync("b.sch").Result, Times.Once);
    }

    [Fact]
    public void UsingDoesNotImportSymbolsTransitively()
    {
        const string LibA = @"
        FUNC INT GET_A()
            RETURN 1
        ENDFUNC
        ";
        const string LibB = @"
        USING 'a.sch'

        FUNC INT GET_B()
            RETURN GET_A()
        ENDFUNC
        ";
        const string Script = @"
        USING 'b.sch'

        SCRIPT my_script
            INT a = GET_A() // error undefined symbol
            INT b = GET_B()
        ENDSCRIPT
        ";

        var a = AnalyzeAndAst(LibA).Ast;
        var usingResolverForLibB = new Mock<IUsingResolver>();
        usingResolverForLibB.Setup(r => r.ResolveUsingAsync("a.sch").Result)
                            .Returns(new UsingResolveResult(UsingResolveStatus.Valid, a));
        var (sb, b) = AnalyzeAndAst(LibB, usingResolverForLibB.Object);
        False(sb.Diagnostics.HasErrors);
        Same(a.Declarations[0], b.FindNthNodeOfType<NameExpression>(1).Semantics.Symbol);

        var usingResolver = new Mock<IUsingResolver>();
        usingResolver.Setup(r => r.ResolveUsingAsync("a.sch").Result)
                     .Returns(new UsingResolveResult(UsingResolveStatus.Valid, a));
        usingResolver.Setup(r => r.ResolveUsingAsync("b.sch").Result)
                     .Returns(new UsingResolveResult(UsingResolveStatus.Valid, b));

        var (s, ast) = AnalyzeAndAst(Script, usingResolver.Object);
        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticUndefinedSymbol, (5, 21), (5, 25), s.Diagnostics);
        Same(b, ast.FindNthNodeOfType<UsingDirective>(1).Semantics.ImportedCompilationUnit);
        Null(ast.FindNthNodeOfType<NameExpression>(1).Semantics.Symbol);
        Same(b.Declarations[0], ast.FindNthNodeOfType<NameExpression>(2).Semantics.Symbol);

        usingResolver.Verify(r => r.ResolveUsingAsync("a.sch").Result, Times.Never);
        usingResolver.Verify(r => r.ResolveUsingAsync("b.sch").Result, Times.Once);
        usingResolverForLibB.Verify(r => r.ResolveUsingAsync("a.sch").Result, Times.Once);
    }

    [Fact]
    public void NonExistantFileInUsingReportsAnError()
    {
        const string Script = @"
        USING 'doesnotexist.sch'

        SCRIPT my_script
        ENDSCRIPT
        ";

        var usingResolver = new Mock<IUsingResolver>();
        usingResolver.Setup(r => r.ResolveUsingAsync("doesnotexist.sch").Result)
                     .Returns(new UsingResolveResult(UsingResolveStatus.NotFound, null));

        var (s, ast) = AnalyzeAndAst(Script, usingResolver.Object);
        True(s.Diagnostics.HasErrors);
        CheckError(ErrorCode.SemanticUsingNotFound, (2, 15), (2, 32), s.Diagnostics);

        usingResolver.Verify(r => r.ResolveUsingAsync("doesnotexist.sch").Result, Times.Once);
    }
}

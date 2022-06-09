namespace ScTools.Tests.ScriptLang.Workspace;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Workspace;

public class ProjectTests
{
    //[Fact]
    //public async Task InitProject()
    //{
    //    var projectConfig = new ProjectConfiguration(
    //        ImmutableArray.Create<BuildConfiguration>(
    //            //  Name           Target                        Defines
    //            new("Debug",   new(Game.GTAV, Platform.x64), ImmutableArray.Create("MY_DEBUG")),
    //            new("Release", new(Game.GTAV, Platform.x64), ImmutableArray.Create("MY_RELEASE"))
    //        ));

    //    await projectConfig.WriteToFileAsync("project01.json");
    //}

    [Fact]
    public async Task ProjectCanBeLoadedAndSourceFilesCanBeAnalyzed()
    {
        using var project = await OpenTestProjectAsync(1);
        var sources = project.Sources;
        var asts = await Task.WhenAll(sources.Select(kvp => kvp.Value.GetAstAsync()));
        var diagnostics = await Task.WhenAll(sources.Select(kvp => kvp.Value.GetDiagnosticsAsync()))!;
        var allDiagnostics = DiagnosticsReport.Combine(diagnostics.Where(d => d is not null)!);

        False(allDiagnostics.HasErrors);
    }

    [Fact]
    public async Task SymbolsFromImportedFileAreAccessible()
    {
        using var project = await OpenTestProjectAsync(1);
        var myMath = project.GetSourceFile("my_math.sch");
        NotNull(myMath);
        var myScript = project.GetSourceFile("my_script.sc");
        NotNull(myScript);

        var myMathAst = await myMath!.GetAstAsync();
        var myScriptAst = await myScript!.GetAstAsync();

        False((await myMath!.GetDiagnosticsAsync())!.HasErrors);
        False((await myScript!.GetDiagnosticsAsync())!.HasErrors);

        var addFuncDecl = myMathAst!.FindFirstNodeOfType<FunctionDeclaration>();
        var importedStaticDecl = myMathAst!.FindFirstNodeOfType<VarDeclaration>();
        var addFuncInvocation = myScriptAst!.FindFirstNodeOfType<InvocationExpression>();
        var myStaticDecl = myScriptAst!.FindFirstNodeOfType<VarDeclaration>();
        Same(addFuncDecl, addFuncInvocation.Callee.GetNameSymbol());
        Same(importedStaticDecl, addFuncInvocation.Arguments[0].GetNameSymbol());
        Same(myStaticDecl, addFuncInvocation.Arguments[1].GetNameSymbol());
    }

    private static Task<Project> OpenTestProjectAsync(int id)
        => Project.OpenProjectAsync($"./Data/project{id:00}/project{id:00}.json");
}

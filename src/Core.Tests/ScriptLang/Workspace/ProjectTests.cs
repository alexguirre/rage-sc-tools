namespace ScTools.Tests.ScriptLang.Workspace;

using ScTools.GameFiles;
using ScTools.ScriptAssembly.Targets.Five;
using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Workspace;

using System.IO;
using System.Text;

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

        var myMathAst = (await myMath!.GetAstAsync())!;
        NotNull(myMathAst);
        var myScriptAst = (await myScript!.GetAstAsync())!;
        NotNull(myScriptAst);

        False((await myMath!.GetDiagnosticsAsync())!.HasErrors);
        False((await myScript!.GetDiagnosticsAsync())!.HasErrors);

        var addFuncDecl = myMathAst.FindFirstNodeOfType<FunctionDeclaration>();
        var importedStaticDecl = myMathAst.FindFirstNodeOfType<VarDeclaration>();
        var addFuncInvocation = myScriptAst.FindFirstNodeOfType<InvocationExpression>();
        var myStaticDecl = myScriptAst.FindFirstNodeOfType<VarDeclaration>();
        Same(addFuncDecl, addFuncInvocation.Callee.GetNameSymbol());
        Same(importedStaticDecl, addFuncInvocation.Arguments[0].GetNameSymbol());
        Same(myStaticDecl, addFuncInvocation.Arguments[1].GetNameSymbol());
    }

    [Fact]
    public async Task CanCompileScriptThatRequiresMultipleSourceFiles()
    {
        using var project = await OpenTestProjectAsync(1);
        var myScript = project.GetSourceFile("my_script.sc")!;
        NotNull(myScript);


        var compilation = (await myScript.CompileAsync())!;
        NotNull(compilation);

        False(compilation.Diagnostics.HasErrors);

        NotNull(compilation.Script);
        AssertAgainstExpectedAssembly(1, "my_script_expected_assembly.gtav.scasm", compilation.Script!);


        project.BuildConfigurationName = "Release-GTAIV";
        compilation = (await myScript.CompileAsync())!;
        NotNull(compilation);

        False(compilation.Diagnostics.HasErrors);

        NotNull(compilation.Script);
        AssertAgainstExpectedAssembly(1, "my_script_expected_assembly.gtaiv.scasm", compilation.Script!);
    }

    [Fact]
    public async Task ImportRelativeToSourceFile()
    {
        using var project = await OpenTestProjectAsync(2);
        var myMath = project.GetSourceFile("src/my_math.sch");
        NotNull(myMath);
        var myScript = project.GetSourceFile("src/my_script.sc");
        NotNull(myScript);

        var myMathAst = (await myMath!.GetAstAsync())!;
        NotNull(myMathAst);
        var myScriptAst = (await myScript!.GetAstAsync())!;
        NotNull(myScriptAst);

        False((await myMath!.GetDiagnosticsAsync())!.HasErrors);
        False((await myScript!.GetDiagnosticsAsync())!.HasErrors);
    }

    private static Task<Project> OpenTestProjectAsync(int id)
        => Project.OpenProjectAsync($"./Data/project{id:00}/project{id:00}.json");

    private static void AssertAgainstExpectedAssembly(int projectId, string expectedAssemblyFileName, IScript compiledScript)
    {
        var expectedAssemblyPath = $"./Data/project{projectId:00}/{expectedAssemblyFileName}";

        using var expectedAssemblyReader = new StreamReader(expectedAssemblyPath, Encoding.UTF8);
        if (compiledScript is GameFiles.Five.Script compiledScriptGTAV)
        {
            var expectedAssembler = Assembler.Assemble(expectedAssemblyReader, expectedAssemblyFileName, options: new() { IncludeFunctionNames = true });

            string sourceDump = compiledScriptGTAV.DumpToString();
            string expectedDump = expectedAssembler.OutputScript.DumpToString();

            Util.AssertScriptsAreEqual(compiledScriptGTAV, expectedAssembler.OutputScript);
        }
        else if (compiledScript is ScriptNY compiledScriptGTAIV)
        {
            string sourceDump = compiledScriptGTAIV.DumpToString();
            // TODO: NY assembly
            ;
        }
    }
}

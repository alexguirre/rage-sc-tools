namespace ScTools.Tests.ScriptLang.Workspace;

using ScTools.ScriptLang.Ast;
using ScTools.ScriptLang.Workspace;

using System.IO;

public class ProjectTests
{
    [Fact]
    public async Task InitProject()
    {
        var projectConfig = new ProjectConfiguration(
            ImmutableArray.Create<BuildConfiguration>(
                //  Name           Target                        Defines
                new("Debug",   new(Game.GTAV, Platform.x64), ImmutableArray.Create("MY_DEBUG")),
                new("Release", new(Game.GTAV, Platform.x64), ImmutableArray.Create("MY_RELEASE"))
            ));

        await projectConfig.WriteToFileAsync("project01.json");
    }

    [Fact]
    public async Task LoadProject()
    {
        using var project = await Project.OpenProjectAsync("./Data/project01/project01.json");
        var sources = project.Sources;
        var asts = await Task.WhenAll(sources.Select(kvp => kvp.Value.GetAstAsync()));
        var diagnostics = await Task.WhenAll(sources.Select(kvp => kvp.Value.GetDiagnosticsAsync()))!;
        var allDiagnostics = DiagnosticsReport.Combine(diagnostics.Where(d => d is not null)!);
        ;
    }
}

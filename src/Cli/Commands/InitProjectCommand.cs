namespace ScTools.Cli.Commands;

using System;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using ScriptLang.Workspace;

internal static class InitProjectCommand
{
    public static readonly Argument<BuildTarget> Target = new(
        "target",
        parse: Parsers.ParseBuildTarget,
        description: "The target game.");

    public static readonly Argument<DirectoryInfo> Root = new(
        "root",
        () => new DirectoryInfo(".\\"),
        "The project root directory.");
    
    public static Command Command { get; } = BuildCommand();

    private static Command BuildCommand()
    {
        var cmd = new Command("init-project")
        {
            Target,
            Root,
        };
        cmd.SetHandler(InvokeAsync, Target, Root);
        return cmd;
    }

    public static Task InvokeAsync(BuildTarget target, DirectoryInfo root)
    {
        var projectFile = Path.Combine(root.FullName, ".project.scproj");
        if (File.Exists(projectFile))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Project already exists at this location: '{projectFile}'");
            Console.ForegroundColor = ConsoleColor.White;
            return Task.FromResult(1);
        }
        
        var config = new ProjectConfiguration(ImmutableArray.Create<BuildConfiguration>(
            new("Debug", target, ImmutableArray.Create<string>("IS_DEBUG_BUILD")),
            new("Release", target, ImmutableArray<string>.Empty)));

        root.Create();
        return config.WriteToFileAsync(projectFile);
    }
}

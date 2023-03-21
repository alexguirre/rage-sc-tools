namespace ScTools.Cli.Commands;

using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScTools.ScriptLang.Workspace;

internal static class InitProjectCommand
{
    public static readonly Argument<string> Name = new(
        "name",
        "The project name.");

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
        var cmd = new Command("init-project", "Create a new RAGE-Script project.")
        {
            Name,
            Target,
            Root,
        };
        cmd.SetHandler(InvokeAsync, Name, Target, Root);
        return cmd;
    }

    public static async Task<int> InvokeAsync(string name, BuildTarget target, DirectoryInfo root)
    {
        var nameSafe = Path.GetInvalidFileNameChars().Aggregate(name, (str, c) => str.Replace(c, '_'));
        var projectFile = Path.Combine(root.FullName, $"{nameSafe}.scproj");
        if (File.Exists(projectFile))
        {
            return Exit.Error($"Project '{name}' already exists at this location: '{projectFile}'");
        }

        var sources = ImmutableArray.Create("src/myscript.sc", "src/mylib.sch");
        var outputPath = "bin/";
        var configs = ImmutableArray.Create<BuildConfiguration>(
            new("Debug", target, ImmutableArray.Create<string>("IS_DEBUG_BUILD")),
            new("Release", target, ImmutableArray<string>.Empty));
        var projectConfig = new ProjectConfiguration(sources, outputPath, configs);

        root.Create();
        var tasks = new System.Collections.Generic.List<Task>(3);
        tasks.Add(projectConfig.WriteToFileAsync(projectFile));

        var srcDir = root.CreateSubdirectory("src");
        var scriptFile = Path.Combine(srcDir.FullName, "myscript.sc");
        var libFile = Path.Combine(srcDir.FullName, "mylib.sch");
        if (!File.Exists(scriptFile))
        {
            tasks.Add(File.WriteAllTextAsync(scriptFile, ScriptTemplate));
        }
        
        if (!File.Exists(libFile))
        {
            tasks.Add(File.WriteAllTextAsync(libFile, LibTemplate));
        }

        await Task.WhenAll(tasks);
        return Exit.Success;
    }

    private const string ScriptTemplate = """
        USING "mylib.sch"

        SCRIPT myscript
            // your code here
            MY_PROC()
        ENDSCRIPT
        """;

    private const string LibTemplate = """
        PROC MY_PROC()
            // your code here
        ENDPROC
        """;
}

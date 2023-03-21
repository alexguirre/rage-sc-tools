namespace ScTools.Cli.Commands;

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

internal static class BuildProjectCommand
{
    public static readonly Argument<FileInfo?> Project = new(
        "project",
        () => null,
        "Path to the project (.scproj). If not provided, the current directory will be searched for a project file.");

    public static readonly Option<string> Config = new(
        new[] { "--config", "-c" },
        () => "Debug",
        "The build configuration.");
    
    public static Command Command { get; } = BuildCommand();

    private static Command BuildCommand()
    {
        var cmd = new Command("build", "Build a RAGE-Script project.")
        {
            Project,
            Config,
        };
        cmd.SetHandler(InvokeAsync, Project, Config);
        return cmd;
    }

    public static async Task<int> InvokeAsync(FileInfo? project, string config)
    {
        if (project is null)
        {
            // user didn't provide a project file, try to find a project in the current directory
            var possibleProjects = Directory.GetFiles(".", "*.scproj");
            switch (possibleProjects.Length)
            {
                case 0:
                    return Exit.Error($"No project located in the current directory.");
                case 1:
                    project = new FileInfo(possibleProjects[0]);
                    break;
                case > 1:
                    return Exit.Error($"Multiple projects found in the current directory. Please specify the project file.");
            }
            Debug.Assert(project is not null);
        }

        if (!project.Exists)
        {
            return Exit.Error($"Project '{project}' does not exist.");
        }

        var p = await ScTools.ScriptLang.Workspace.Project.OpenProjectAsync(project.FullName);
        if (p.Configuration.GetBuildConfiguration(config) is null)
        {
            return Exit.Error($"""
            Build configuration '{config}' does not exist.
            Available configurations:
            {string.Join(Environment.NewLine, p.Configuration.Configurations.Select(c => $"  - {c.Name}"))}
            """);
        }

        p.BuildConfigurationName = config;
        var compileTasks = p.Sources.Select(s => s.Value.CompileAsync());

        var outputDir = new DirectoryInfo(Path.Combine(p.RootDirectory, p.Configuration.OutputPath));
        outputDir.Create();
        outputDir = outputDir.CreateSubdirectory(config);
        
        var results = await Task.WhenAll(compileTasks);
        var tasks = new List<Task>(results.Length);
        foreach (var result in results)
        {
            if (result is null)
            {
                continue;
            }

            var sourcePath = Path.GetRelativePath(p.RootDirectory, result.Source.Path);
            if (result.Diagnostics.HasErrors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Compilation of '{sourcePath}' failed:");
                result.Diagnostics.PrintAll(Console.Error);
                Console.Error.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"Compilation of '{sourcePath}' succeeded.");
                if (result.Script is not null)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.White;

                    var outputFile = Path.Combine(outputDir.FullName, $"{Path.GetFileNameWithoutExtension(sourcePath)}.sco");
                    Console.WriteLine($"  Writing '{Path.GetRelativePath(p.RootDirectory, outputFile)}'");
                    switch (result.Script)
                    {
                        case GameFiles.Five.Script scriptGTAV:
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                var ysc = new GameFiles.Five.YscFile { Script = scriptGTAV };
                                var data = ysc.Save(Path.GetFileName(outputFile));
                                File.WriteAllBytes(outputFile, data);
                            }));
                        }
                        break;

                        case GameFiles.ScriptNY scriptGTAIV:
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                GameFiles.Keys.NY.Load("D:\\programs\\SteamLibrary\\steamapps\\common\\Grand Theft Auto IV\\GTAIV\\GTAIV.exe");
                                using var outputStream = new FileStream(outputFile, FileMode.Create);
                                scriptGTAIV.Magic = GameFiles.ScriptNY.MagicEncrypted;
                                scriptGTAIV.Write(new CodeWalker.GameFiles.DataWriter(outputStream), GameFiles.Keys.NY.AesKeyPC);
                            }));
                        }
                        break;
                        
                        default:
                            throw new InvalidOperationException("Unsupported script type");
                    }
                }
                else
                {
                    Console.WriteLine(" No script was generated.");
                }
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        await Task.WhenAll(tasks);
        return Exit.Success;
    }
}

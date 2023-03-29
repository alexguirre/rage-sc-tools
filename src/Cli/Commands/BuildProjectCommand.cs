namespace ScTools.Cli.Commands;

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScTools.ScriptLang.Workspace;
using Spectre.Console;
using Spectre.Console.Rendering;

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
        var stopwatch = Stopwatch.StartNew();

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

        async Task<SourceFile.CompilationResult?> CompileAndReportProgressAsync(SourceFile sf, ProgressTask progress)
        {
            var res = await sf.CompileAsync();
            progress.Increment(1);
            return res;
        }
        var compileTask = Std.Out.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn() ,
                new CounterColumn(),
                new SpinnerColumn()
            ).StartAsync(async ctx =>
            {
                var progress = ctx.AddTask("Compiling scripts")
                    .MaxValue(p.Sources.Count)
                    .Value(0);
                progress.StartTask();
                var tasks = p.Sources.Select(s => CompileAndReportProgressAsync(s.Value, progress));
                var results = await Task.WhenAll(tasks);
                progress.StopTask();
                return results;
            });

        var outputDir = new DirectoryInfo(Path.Combine(p.RootDirectory, p.Configuration.OutputPath));
        outputDir.Create();
        outputDir = outputDir.CreateSubdirectory(config);

        var keys = Program.Keys;

        var results = await compileTask;
        var warningCount = 0;
        var errorCount = 0;
        var scriptCount = 0;
        var tasks = new List<Task>(results.Length);
        foreach (var result in results)
        {
            Debug.Assert(result is not null);

            var sourcePath = Path.GetRelativePath(p.RootDirectory, result.Source.Path);
            if (result.Diagnostics.HasErrors)
            {
                Std.Err.MarkupLineInterpolated($"[red]Compilation of '{sourcePath}' failed:[/]");
                Std.Err.WriteDiagnostics(result.Diagnostics);
                Std.Err.WriteLine();
            }
            else
            {
                Std.Out.MarkupLineInterpolated($"[green]Compilation of '{sourcePath}' succeeded:[/]");
                Std.Out.WriteDiagnostics(result.Diagnostics); // in case of warnings
                if (result.Script is not null)
                {
                    var outputFile = Path.Combine(outputDir.FullName, $"{Path.GetFileNameWithoutExtension(sourcePath)}.sco");
                    Std.Out.WriteLine($"  Writing '{Path.GetRelativePath(p.RootDirectory, outputFile)}'.");
                    scriptCount++;
                    switch (result.Script)
                    {
                        case GameFiles.GTA5.Script scriptGTAV:
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                var ysc = new GameFiles.GTA5.YscFile { Script = scriptGTAV };
                                var data = ysc.Save(Path.GetFileName(outputFile), keys.GTA5.NgPC);
                                File.WriteAllBytes(outputFile, data);
                            }));
                        }
                        break;

                        case GameFiles.GTA4.Script scriptGTAIV:
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                using var w = new BinaryWriter( new FileStream(outputFile, FileMode.Create));
                                scriptGTAIV.Magic = GameFiles.GTA4.Script.MagicEncrypted;
                                scriptGTAIV.Write(w, keys.GTA4.AesKeyPC);
                            }));
                        }
                        break;

                        case GameFiles.MP3.Script scriptMP3:
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                using var w = new BinaryWriter(new FileStream(outputFile, FileMode.Create));
                                scriptMP3.Magic = GameFiles.MP3.Script.MagicEncryptedCompressedV17;
                                scriptMP3.Unknown_18h = 0xFFFFFFFF;
                                scriptMP3.Write(w, keys.MP3.AesKeyPC!);
                            }));
                        }
                        break;

                        default:
                            throw new InvalidOperationException("Unsupported script type");
                    }
                }
                else
                {
                    Std.Out.WriteLine("  No script was generated.");
                }
                Std.Out.WriteLine();
            }

            warningCount += result.Diagnostics.Warnings.Count;
            errorCount += result.Diagnostics.Errors.Count;
        }

        await Task.WhenAll(tasks);

        if (errorCount != 0)
        {
            Std.Out.MarkupLine("[red]Build FAILED.[/]");
        }
        else
        {
            Std.Out.MarkupLine("[green]Build SUCCEEDED.[/]");
        }
        Std.Out.WriteLine();
        Std.Out.WriteLine($"  {warningCount} Warning(s)");
        Std.Out.WriteLine($"  {errorCount} Error(s)");
        Std.Out.WriteLine();
        Std.Out.WriteLine($"  {scriptCount} Script(s) generated");
        Std.Out.WriteLine();

        stopwatch.Stop();
        Std.Out.MarkupLineInterpolated($"Time Elapsed [blue]{stopwatch.Elapsed}[/]");
        
        return Exit.Success;
    }

    /// <summary>
    /// A column showing a counter with the current and maximum value (`{Value}/{MaxValue}`).
    /// </summary>
    private sealed class CounterColumn : ProgressColumn
    {
        private readonly Style completedStyle = new(foreground: Color.Green);

        /// <inheritdoc/>
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            var style = task.IsFinished ? completedStyle : Style.Plain;
            return new Text($"{(long)task.Value}/{(long)task.MaxValue}", style).RightJustified();
        }
    }
}

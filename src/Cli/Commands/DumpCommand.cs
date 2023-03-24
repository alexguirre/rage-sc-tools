namespace ScTools.Cli.Commands;

using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeWalker.GameFiles;
using ScTools.GameFiles;
using ScTools.ScriptLang.Workspace;
using Spectre.Console;

internal static class DumpCommand
{
    public static readonly Argument<FileGlob[]> Input = new Argument<FileGlob[]>(
            "input",
            parse: Parsers.ParseFileGlobs,
            description: "The input script files. Supports glob patterns.")
            .AtLeastOne();
    public static  readonly Option<DirectoryInfo> Output = new Option<DirectoryInfo>(
            new[] { "--output", "-o" },
            () => new DirectoryInfo(".\\"),
            "The output directory.")
            .ExistingOnly();
    public static readonly Option<BuildTarget> Target = new Option<BuildTarget>(
            new[] { "--target", "-t" },
            parseArgument: Parsers.ParseBuildTarget,
            description: "The target game.");
    public static Command Command { get; } = BuildCommand();

    private static Command BuildCommand()
    {
        var cmd = new Command("dump", "Basic disassembly of script files.")
        {
            Input,
            Output,
            Target,
        };
        cmd.SetHandler(InvokeAsync, Input, Output, Target);
        return cmd;
    }

    public static async Task InvokeAsync(FileGlob[] input, DirectoryInfo output, BuildTarget target)
    {
        static void Print(string str)
        {
            lock (Command)
            {
                Std.Out.WriteLine(str);
            }
        }

        var totalTime = Stopwatch.StartNew();

        var keys = Program.Keys;

        var inputFiles = input.SelectMany(i => i.Matches);
        await Parallel.ForEachAsync(inputFiles, async (inputFile, cancellationToken) =>
        {
            try
            {
                var extension = "dump.txt";
                var outputFile = new FileInfo(Path.Combine(output.FullName, Path.ChangeExtension(inputFile.Name, extension)));

                Print($"Dumping '{inputFile}'...");
                var source = await File.ReadAllBytesAsync(inputFile.FullName, cancellationToken);

                IScript script;
                switch (target)
                {
                    case (Game.GTA5, Platform.x64):
                        throw new NotImplementedException("GTAV x64 not supported");

                    case (Game.MC4, Platform.Xenon):
                    case (Game.GTA4, Platform.x86):
                    {
                        byte[]? aesKey = target.Game switch
                        {
                            Game.GTA4 => keys.GTA4.AesKeyPC,
                            Game.MC4 => keys.MC4.AesKeyXenon,
                            _ => throw new UnreachableException("Game already restricted by parent switch")
                        };

                        var sc = new GameFiles.GTA4.Script();
                        sc.Read(new DataReader(new MemoryStream(source)), aesKey);
                        script = sc;
                        break;
                    }
                    case (Game.RDR2, Platform.Xenon):
                    {
                        var sc = new ScriptRDR2();
                        sc.Read(new DataReader(new MemoryStream(source), Endianess.BigEndian), keys.RDR2.AesKeyXenon);
                        script = sc;
                        break;
                    }
                    case (Game.MP3, Platform.x86):
                    {
                        var sc = new GameFiles.MP3.Script();
                        sc.Read(new DataReader(new MemoryStream(source)), keys.MP3.AesKeyPC!); // AES key checked for null inside Read
                        script = sc;
                        break;
                    }
                    
                    default:
                        throw new InvalidOperationException($"Unsupported build target '{target}'");
                }

                await using var outputStream = outputFile.Open(FileMode.Create);
                await using var outputWriter = new StreamWriter(outputStream);
                script.Dump(outputWriter, DumpOptions.Default);
            }
            catch (Exception e)
            {
                lock (Command)
                {
                    Std.Err.MarkupLineInterpolated($"[red]Unhandled error:[/]");
                    Std.Err.WriteException(e);
                }
            }
        });

        totalTime.Stop();
        Print($"Total time: {totalTime.Elapsed}");
    }
}

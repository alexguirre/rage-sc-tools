namespace ScTools.Cli.Commands;

using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScTools.GameFiles;
using ScTools.GameFiles.GTA5;
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

    public static readonly Option<bool> IR = new("--ir", "Include an intermediate representation of the disassembly.");

    public static Command Command { get; } = BuildCommand();

    private static Command BuildCommand()
    {
        var cmd = new Command("dump", "Basic disassembly of script files.")
        {
            Input,
            Output,
            Target,
            IR,
        };
        cmd.SetHandler(InvokeAsync, Input, Output, Target, IR);
        return cmd;
    }

    public static async Task InvokeAsync(FileGlob[] input, DirectoryInfo output, BuildTarget target, bool ir)
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
                var outputFile =
                    new FileInfo(Path.Combine(output.FullName, Path.ChangeExtension(inputFile.Name, extension)));

                Print($"Dumping '{inputFile}'...");
                var source = await File.ReadAllBytesAsync(inputFile.FullName, cancellationToken);

                IScript script;
                switch (target)
                {
                    case (Game.GTA5, Platform.x64):
                    {
                        var ysc = new GameFiles.GTA5.YscFile();
                        ysc.Load(source, inputFile.Name, keys.GTA5.NgPC);
                        script = ysc.Script;
                        break;
                    }
                    case (Game.MC4, Platform.Xenon):
                    case (Game.GTA4, Platform.x86):
                    {
                        byte[]? aesKey = target.Game switch
                        {
                            Game.GTA4 => keys.GTA4.AesKeyPC,
                            Game.MC4 => keys.MC4.AesKeyXenon,
                            _ => throw new UnreachableException("Game already restricted by parent switch")
                        };

                        using var r = new BinaryReader(new MemoryStream(source));
                        var sc = new GameFiles.GTA4.Script();
                        sc.Read(r, aesKey);
                        script = sc;
                        break;
                    }
                    case (Game.RDR2, Platform.Xenon):
                    {
                        using var r = new BigEndianBinaryReader(new MemoryStream(source));
                        var sc = new GameFiles.RDR2.Script();
                        sc.Read(r, keys.RDR2.AesKeyXenon);
                        script = sc;
                        break;
                    }
                    case (Game.MP3, Platform.x86):
                    {
                        using var r = new BinaryReader(new MemoryStream(source));
                        var sc = new GameFiles.MP3.Script();
                        sc.Read(r, keys.MP3.AesKeyPC!); // AES key checked for null inside Read
                        script = sc;
                        break;
                    }

                    default:
                        throw new InvalidOperationException($"Unsupported build target '{target}'");
                }

                await using var outputWriter = new StreamWriter(outputFile.Open(FileMode.Create));
                script.Dump(outputWriter, DumpOptions.Default with { IncludeIR = ir });
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

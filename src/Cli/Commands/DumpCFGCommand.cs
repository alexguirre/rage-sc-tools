namespace ScTools.Cli.Commands;

using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ScTools.GameFiles;
using ScTools.ScriptLang.Workspace;
using Spectre.Console;

internal static class DumpCFGCommand
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

    public static readonly Option<bool> Image = new(new[] { "--image", "-i" }, "Use Graphviz to generate an image from the dot file. Requires Graphviz to be installed.");

    public static Command Command { get; } = BuildCommand();

    private static Command BuildCommand()
    {
        var cmd = new Command("dump-cfg", "Dumps the control flow graph of the scripts as a DOT graph file.")
        {
            Input,
            Output,
            Target,
            Image,
        };
        cmd.SetHandler(InvokeAsync, Input, Output, Target, Image);
        return cmd;
    }

    public static async Task<int> InvokeAsync(FileGlob[] input, DirectoryInfo output, BuildTarget target, bool image)
    {
        static void Print(string str)
        {
            lock (Command)
            {
                Std.Out.WriteLine(str);
            }
        }

        // var sb = new StringBuilder(260);
        if (image && SearchPath(null, "dot.exe", null, 0, null, IntPtr.Zero) == 0)
        {
            return Exit.Error("dot.exe not found in PATH. Please install Graphviz from [link]https://graphviz.org/download/[/].");
        }
        
        var totalTime = Stopwatch.StartNew();

        var keys = Program.Keys;

        var inputFiles = input.SelectMany(i => i.Matches);
        await Parallel.ForEachAsync(inputFiles, async (inputFile, cancellationToken) =>
        {
            try
            {
                var extension = "cfg.dot";
                var outputFile =
                    new FileInfo(Path.Combine(output.FullName, Path.ChangeExtension(inputFile.Name, extension)));

                Print($"Dumping '{inputFile}'...");
                var source = await File.ReadAllBytesAsync(inputFile.FullName, cancellationToken);

                Decompiler.Script script;
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

                        using var r = new BinaryReader(new MemoryStream(source));
                        var sc = new GameFiles.GTA4.Script();
                        sc.Read(r, aesKey);
                        script = Decompiler.Script.FromGTA4(sc);
                        break;
                    }
                    case (Game.RDR2, Platform.Xenon):
                    {
                        using var r = new BigEndianBinaryReader(new MemoryStream(source));
                        var sc = new GameFiles.RDR2.Script();
                        sc.Read(r, keys.RDR2.AesKeyXenon);
                        script = Decompiler.Script.FromRDR2(sc);
                        break;
                    }
                    case (Game.MP3, Platform.x86):
                    {
                        using var r = new BinaryReader(new MemoryStream(source));
                        var sc = new GameFiles.MP3.Script();
                        sc.Read(r, keys.MP3.AesKeyPC!); // AES key checked for null inside Read
                        script = Decompiler.Script.FromMP3(sc);
                        break;
                    }

                    default:
                        throw new InvalidOperationException($"Unsupported build target '{target}'");
                }

                await using (var outputWriter = new StreamWriter(outputFile.Open(FileMode.Create)))
                {
                    Decompiler.CFGGraphViz.ToDot(outputWriter, script.EntryFunction.RootBlock);
                }

                if (image)
                {
                    var dotArgs = new[] { "-Tpng", "-o", Path.ChangeExtension(outputFile.FullName,".png"), outputFile.FullName };
                    var dot = Process.Start("dot.exe", dotArgs);
                    await dot.WaitForExitAsync(cancellationToken);
                }
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
        return Exit.Success;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern uint SearchPath(string? lpPath, string lpFileName, string? lpExtension, int nBufferLength,
                                          [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpBuffer, IntPtr lpFilePart);
}

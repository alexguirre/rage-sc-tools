namespace ScTools.Cli.Commands;

using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeWalker.GameFiles;
using GameFiles;
using GameFiles.GTA5;
using ScTools.ScriptLang;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Workspace;
using Spectre.Console;

internal static class CompileCommand
{
    public static readonly Argument<FileGlob[]> Input = new Argument<FileGlob[]>(
            "input",
            parse: Parsers.ParseFileGlobs,
            description: "The input RAGE-Script files (.sc). Supports glob patterns.")
            .AtLeastOne();
    public static  readonly Option<DirectoryInfo> Output = new Option<DirectoryInfo>(
            new[] { "--output", "-o" },
            () => new DirectoryInfo(".\\"),
            "The output directory.")
            .ExistingOnly();
    public static  readonly Option<DirectoryInfo[]> Include = new Option<DirectoryInfo[]>(
            new[] { "--include", "-I" },
            () => Array.Empty<DirectoryInfo>(),
            "Additional directories to lookup for source files imported with USINGs.")
            .ExistingOnly();
    public static readonly Option<BuildTarget> Target = new Option<BuildTarget>(
            new[] { "--target", "-t" },
            parseArgument: Parsers.ParseBuildTarget,
            description: "The target game.");
    public static readonly Option<bool> Unencrypted = new Option<bool>(
            new[] { "--unencrypted", "-u" },
            "Output unencrypted files of the compiled scripts.");
    public static Command Command { get; } = BuildCommand();

    private static Command BuildCommand()
    {
        var cmd = new Command("compile", "Compile RAGE-Script files.")
        {
            Input,
            Output,
            Include,
            Target,
            Unencrypted,
        };
        cmd.SetHandler(InvokeAsync, Input, Output, Include, Target, Unencrypted);
        return cmd;
    }

    public static async Task InvokeAsync(FileGlob[] input, DirectoryInfo output, DirectoryInfo[] include, BuildTarget target, bool unencrypted)
    {
        static void Print(string str)
        {
            lock (Command)
            {
                Std.Out.WriteLine(str);
            }
        }

        var totalTime = Stopwatch.StartNew();

        Keys.LoadAll();

        var inputFiles = input.SelectMany(i => i.Matches);
        await Parallel.ForEachAsync(inputFiles, async (inputFile, cancellationToken) =>
        {
            var extension = target switch
            {
                (Game.GTA4, Platform.x86) => "sco",
                (Game.GTA5, Platform.x64) => "ysc",
                _ => throw new NotImplementedException($"Unsupported build target '{target}'"),
            };

            try
            {
                var outputFile = new FileInfo(Path.Combine(output.FullName, Path.ChangeExtension(inputFile.Name, extension)));

                Print($"Compiling '{inputFile}'...");
                var source = await File.ReadAllTextAsync(inputFile.FullName, cancellationToken);
                var d = new DiagnosticsReport();
                var l = new Lexer(inputFile.FullName, source, d);
                var p = new ScriptLang.Parser(l, d, new(d));
                var s = new SemanticsAnalyzer(d);

                var u = p.ParseCompilationUnit();
                cancellationToken.ThrowIfCancellationRequested();
                u.Accept(s);
                cancellationToken.ThrowIfCancellationRequested();

                if (!d.HasErrors)
                {
                    var compiledScript = ScriptCompiler.Compile(u, target);
                    if (compiledScript is null)
                    {
                        throw new InvalidOperationException("No SCRIPT declaration found");
                    }

                    var outputFileName = outputFile.FullName;
                    Print($"Successful compilation, writing '{Path.GetRelativePath(Directory.GetCurrentDirectory(), outputFileName)}'...");
                    switch (compiledScript)
                    {
                        case ScTools.GameFiles.GTA5.Script scriptGTAV:
                        {
                            var ysc = new YscFile { Script = scriptGTAV };
                            var data = ysc.Save(Path.GetFileName(outputFileName));
                            var t1 = File.WriteAllBytesAsync(outputFileName, data, cancellationToken);

                            Task? t2 = null;
                            if (unencrypted)
                            {
                                data = ysc.Save();
                                t2 = File.WriteAllBytesAsync(Path.ChangeExtension(outputFileName, "unencrypted.ysc"),
                                    data, cancellationToken);
                            }

                            await Task.WhenAll(t2 is null ? new[] { t1 } : new[] { t1, t2 });
                        }
                        break;

                        case ScTools.GameFiles.GTA4.Script scriptGTAIV:
                        {
                            await using var outputStream = outputFile.OpenWrite();
                            scriptGTAIV.Magic = ScTools.GameFiles.GTA4.Script.MagicEncrypted;
                            scriptGTAIV.Write(new DataWriter(outputStream), Keys.GTA4.AesKeyPC);

                            if (unencrypted)
                            {
                                var outputFileUnencrypted = new FileInfo(Path.ChangeExtension(outputFileName, "unencrypted.sco"));
                                await using var outputStreamUnencrypted = outputFileUnencrypted.OpenWrite();
                                scriptGTAIV.Magic = ScTools.GameFiles.GTA4.Script.MagicUnencrypted;
                                scriptGTAIV.Write(new DataWriter(outputStreamUnencrypted), Keys.GTA4.AesKeyPC);
                            }
                        }
                        break;
                        
                        default:
                            throw new InvalidOperationException("Unsupported script type");
                    }
                }
                else
                {
                    lock (Command)
                    {
                        Std.Err.MarkupLineInterpolated($"[red]Compilation of '{inputFile}' failed:[/]");
                        Std.Err.WriteDiagnostics(d);
                        Std.Err.WriteLine();
                    }
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
    }
}

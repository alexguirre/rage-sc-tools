namespace ScTools.Cli.Commands;

using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeWalker.GameFiles;
using GameFiles;
using GameFiles.Five;
using ScTools.ScriptLang;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Workspace;

internal static class CompileCommand
{
    public static readonly Argument<FileGlob[]> Input = new Argument<FileGlob[]>(
            "input",
            parse: Parsers.ParseFileGlobs,
            description: "The input SC files. Supports glob patterns.")
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
        var cmd = new Command("compile")
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
            lock (Console.Out)
            {
                Console.WriteLine(str);
            }
        }

        var totalTime = Stopwatch.StartNew();

        var inputFiles = input.SelectMany(i => i.Matches);
        await Parallel.ForEachAsync(inputFiles, async (inputFile, cancellationToken) =>
        {
            var extension = target switch
            {
                (Game.GTAIV, Platform.x86) => "sco",
                (Game.GTAV, Platform.x64) => "ysc",
                _ => throw new NotImplementedException("Unsupported build target"),
            };
            
            var outputFile = new FileInfo(Path.Combine(output.FullName, Path.ChangeExtension(inputFile.Name, extension)));
            try
            {
                Print($"Compiling '{inputFile}'...");
                var source = await File.ReadAllTextAsync(inputFile.FullName, cancellationToken);
                var d = new DiagnosticsReport();
                var l = new Lexer(inputFile.FullName, source, d);
                var p = new ScriptLang.Parser(l, d);
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
                        case Script scriptGTAV:
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

                        case ScriptNY scriptGTAIV:
                        {
                            Keys.NY.Load("D:\\programs\\SteamLibrary\\steamapps\\common\\Grand Theft Auto IV\\GTAIV\\GTAIV.exe");
                            await using var outputStream = outputFile.OpenWrite();
                            scriptGTAIV.Magic = ScriptNY.MagicEncrypted;
                            scriptGTAIV.Write(new DataWriter(outputStream), Keys.NY.AesKeyPC);

                            if (unencrypted)
                            {
                                var outputFileUnencrypted = new FileInfo(Path.ChangeExtension(outputFileName, "unencrypted.sco"));
                                await using var outputStreamUnencrypted = outputFileUnencrypted.OpenWrite();
                                scriptGTAIV.Magic = ScriptNY.MagicUnencrypted;
                                scriptGTAIV.Write(new DataWriter(outputStreamUnencrypted), Keys.NY.AesKeyPC);
                            }
                        }
                        break;
                        
                        default:
                            throw new InvalidOperationException("Unsupported script type");
                    }
                }
                else
                {
                    lock (Console.Error)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine($"Compilation of '{inputFile}' failed:");
                        d.PrintAll(Console.Error);
                        Console.Error.WriteLine();
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }
            catch (Exception e)
            {
                lock (Console.Error)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.Write(e.ToString());
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        });

        totalTime.Stop();
        Print($"Total time: {totalTime.Elapsed}");
    }
}

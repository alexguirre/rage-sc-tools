namespace ScTools.Cli
{
    using System;
    using System.Text;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Diagnostics;
    using CodeWalker.GameFiles;
    using ScTools.GameFiles;
    using System.Threading.Tasks;
    using System.Linq;
    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang;
    using System.Collections.Generic;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.CodeGen;

    internal static class Program
    {
        private static int Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            var rootCmd = new RootCommand("Tool for working with Grand Theft Auto V script files (.ysc).");

            Command dump = new Command("dump")
            {
                new Argument<FileGlob[]>(
                    "input",
                    "The input YSC files. Supports glob patterns.")
                    .AtLeastOne(),
                new Option<FileInfo>(
                    new[] { "--output", "-o" },
                    "The output file. If not specified, the dump is printed to the console.")
                    .LegalFilePathsOnly(),
                new Option("--no-metadata", "Do not include the script metadata."),
                new Option("--no-disassembly", "Do not include the script disassembly."),
                new Option("--no-bytes", "Do not include the instruction bytes in the disassembly."),
                new Option("--no-offsets", "Do not include the instruction offsets in the disassembly."),
                new Option("--no-instructions", "Do not include the instruction textual representation in the disassembly."),
            };
            dump.Handler = CommandHandler.Create<DumpOptions>(Dump);

            Command disassemble = new Command("disassemble")
            {
                new Argument<FileGlob[]>(
                    "input",
                    "The input YSC files. Supports glob patterns.")
                    .AtLeastOne(),
                new Option<DirectoryInfo>(
                    new[] { "--output", "-o" },
                    () => new DirectoryInfo(".\\"),
                    "The output directory.")
                    .ExistingOnly(),
                new Option<FileInfo>(
                    new[] { "--nativedb", "-n" },
                    "The JSON file containing the native commands definitions.")
                    .ExistingOnly(),
            };
            disassemble.Handler = CommandHandler.Create<DisassembleOptions>(Disassemble);

            Command disassemblerE2E = new Command("disassembler-e2e", "Tests disassembling and re-assembling scripts")
            {
                new Argument<FileGlob[]>(
                    "input",
                    "The input YSC files. Supports glob patterns.")
                    .AtLeastOne(),
                new Option<DirectoryInfo>(
                    new[] { "--output", "-o" },
                    "If specified, output the disassembly and dump files to the specified directory.")
                    .ExistingOnly(),
                new Option<FileInfo>(
                    new[] { "--nativedb", "-n" },
                    "The JSON file containing the native commands definitions.")
                    .ExistingOnly(),
                new Option(
                    new[] { "--debug", "-d" },
                    "If specified, re-assemble the scripts with debug options.")
            };
            disassemblerE2E.Handler = CommandHandler.Create<DisassemblerE2EOptions>(DisassemblerE2E);

            Command assemble = new Command("assemble")
            {
                new Argument<FileGlob[]>(
                    "input",
                    "The input SCASM files. Supports glob patterns.")
                    .AtLeastOne(),
                new Option<DirectoryInfo>(
                    new[] { "--output", "-o" },
                    () => new DirectoryInfo(".\\"),
                    "The output directory.")
                    .ExistingOnly(),
                new Option<FileInfo>(
                    new[] { "--nativedb", "-n" },
                    "The JSON file containing the native commands definitions.")
                    .ExistingOnly(),
                new Option(new[] { "--function-names", "-f" }, "Include the function names in ENTER instructions."),
                new Option(new[] { "--unencrypted", "-u" }, "Output unencrypted files of the assembled scripts."),
            };
            assemble.Handler = CommandHandler.Create<AssembleOptions>(Assemble);

            Command compile = new Command("compile")
            {
                new Argument<FileGlob[]>(
                    "input",
                    "The input SC files. Supports glob patterns.")
                    .AtLeastOne(),
                new Option<DirectoryInfo>(
                    new[] { "--output", "-o" },
                    () => new DirectoryInfo(".\\"),
                    "The output directory.")
                    .ExistingOnly(),
                new Option<FileInfo>(
                    new[] { "--nativedb", "-n" },
                    "The JSON file containing the native commands definitions.")
                    .ExistingOnly(),
                new Option(new[] { "--unencrypted", "-u" }, "Output unencrypted files of the compiled scripts."),
            };
            compile.Handler = CommandHandler.Create<CompileOptions>(Compile);

            Command genNatives = new Command("gen-natives")
            {
                new Argument<FileInfo>(
                    "nativedb",
                    "The JSON file containing the native commands definitions.")
                    .ExistingOnly(),
            };
            genNatives.Handler = CommandHandler.Create<FileInfo>(GenNatives);

            Command fetchNativeDb = new Command("fetch-nativedb")
            {
                new Option<Uri>(
                    new[] { "--shv-url", "-s" },
                    () => new Uri("http://www.dev-c.com/files/ScriptHookV_1.0.2215.0.zip"),
                    "Specifies the URL from which to download the ScriptHookV release .zip."),
                new Option<Uri>(
                    new[] { "--nativedb-url", "-n" },
                    () => new Uri("https://raw.githubusercontent.com/alloc8or/gta5-nativedb-data/master/natives.json"),
                    "Specifies the URL from which to download the native DB data."),
                new Option<FileInfo>(
                    new[] { "--output", "-o" },
                    () => new FileInfo("nativesdb.json"),
                    "The output JSON file.")
                    .LegalFilePathsOnly(),
            };
            fetchNativeDb.Handler = CommandHandler.Create<FetchNativeDbOptions>(FetchNativeDb);

            rootCmd.AddCommand(dump);
            rootCmd.AddCommand(disassemble);
            rootCmd.AddCommand(disassemblerE2E);
            rootCmd.AddCommand(assemble);
            rootCmd.AddCommand(compile);
            rootCmd.AddCommand(genNatives);
            rootCmd.AddCommand(fetchNativeDb);

            return rootCmd.InvokeAsync(args).Result;
        }

        private static void LoadGTA5Keys()
        {
            string path = ".\\Keys";
            GTA5Keys.PC_AES_KEY = File.ReadAllBytes(path + "\\gtav_aes_key.dat");
            GTA5Keys.PC_NG_KEYS = CryptoIO.ReadNgKeys(path + "\\gtav_ng_key.dat");
            GTA5Keys.PC_NG_DECRYPT_TABLES = CryptoIO.ReadNgTables(path + "\\gtav_ng_decrypt_tables.dat");
            GTA5Keys.PC_NG_ENCRYPT_TABLES = CryptoIO.ReadNgTables(path + "\\gtav_ng_encrypt_tables.dat");
            GTA5Keys.PC_NG_ENCRYPT_LUTs = CryptoIO.ReadNgLuts(path + "\\gtav_ng_encrypt_luts.dat");
            GTA5Keys.PC_LUT = File.ReadAllBytes(path + "\\gtav_hash_lut.dat");
        }

        private class AssembleOptions
        {
            public FileGlob[] Input { get; set; }
            public DirectoryInfo Output { get; set; }
            public FileInfo NativeDB { get; set; }
            public bool FunctionNames { get; set; }
            public bool Unencrypted { get; set; }
        }

        private static void Assemble(AssembleOptions options)
        {
            static void Print(string str)
            {
                lock (Console.Out)
                {
                    Console.WriteLine(str);
                }
            }

            LoadGTA5Keys();

            NativeDB nativeDB = null;
            if (options.NativeDB != null)
            {
                nativeDB = NativeDB.FromJson(File.ReadAllText(options.NativeDB.FullName));
            }

            Parallel.ForEach(options.Input.SelectMany(i => i.Matches), inputFile =>
            {
                var outputFile = new FileInfo(Path.Combine(options.Output.FullName, Path.ChangeExtension(inputFile.Name, "ysc")));

                YscFile ysc = new YscFile();
                try
                {
                    Print($"Assembling '{inputFile}'...");
                    const int BufferSize = 1024 * 1024 * 32; // 32mb
                    using var reader = new StreamReader(inputFile.OpenRead(), Encoding.UTF8, bufferSize: BufferSize, leaveOpen: false);
                    var asm = Assembler.Assemble(reader, inputFile.Name, nativeDB);
                    if (asm.Diagnostics.HasErrors)
                    {
                        lock (Console.Error)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.WriteLine($"Assembly of '{inputFile}' failed:");
                            asm.Diagnostics.PrintAll(Console.Error);
                            Console.Error.WriteLine();
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        return;
                    }

                    ysc.Script = asm.OutputScript;
                }
                catch (Exception e)
                {
                    lock (Console.Error)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.Write(e.ToString());
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    return;
                }

                Print($"Writing '{inputFile}'...");
                byte[] data = ysc.Save(Path.GetFileName(outputFile.FullName));
                File.WriteAllBytes(outputFile.FullName, data);

                if (options.Unencrypted)
                {
                    data = ysc.Save();
                    File.WriteAllBytes(Path.ChangeExtension(outputFile.FullName, "unencrypted.ysc"), data);
                }
            });
        }

        private class CompileOptions
        {
            public FileGlob[] Input { get; set; }
            public DirectoryInfo Output { get; set; }
            public FileInfo NativeDB { get; set; }
            public bool Unencrypted { get; set; }
        }

        private static void Compile(CompileOptions options)
        {
            static void Print(string str)
            {
                lock (Console.Out)
                {
                    Console.WriteLine(str);
                }
            }

            var totalTime = Stopwatch.StartNew();

            LoadGTA5Keys();

            NativeDB nativeDB = NativeDB.Empty;
            if (options.NativeDB != null)
            {
                nativeDB = NativeDB.FromJson(File.ReadAllText(options.NativeDB.FullName));
            }

            Parallel.ForEach(options.Input.SelectMany(i => i.Matches), inputFile =>
            {
                var outputFile = new FileInfo(Path.Combine(options.Output.FullName, Path.ChangeExtension(inputFile.Name, "ysc")));

                //try
                //{
                //    Print($"Compiling '{inputFile}'...");
                //    using var sourceReader = new StreamReader(inputFile.OpenRead());
                //    var d = new DiagnosticsReport();
                //    var p = new Parser(sourceReader, inputFile.FullName) { UsingResolver = new FileUsingResolver() };
                //    p.Parse(d);

                //    // TODO: refactor this
                //    var globalSymbols = GlobalSymbolTableBuilder.Build(p.OutputAst, d);
                //    IdentificationVisitor.Visit(p.OutputAst, d, globalSymbols, nativeDB);
                //    TypeChecker.Check(p.OutputAst, d, globalSymbols);

                //    var sourceAssembly = "";
                //    if (!d.HasErrors)
                //    {
                //        using var sink = new StringWriter();
                //        new CodeGenerator(sink, p.OutputAst, globalSymbols, d, nativeDB).Generate();
                //        sourceAssembly = sink.ToString();
                //    }


                //    if (!d.HasErrors)
                //    {
                //        using var sourceAssemblyReader = new StringReader(sourceAssembly);
                //        var sourceAssembler = Assembler.Assemble(sourceAssemblyReader, Path.ChangeExtension(inputFile.FullName, "scasm"), nativeDB, options: new() { IncludeFunctionNames = true });

                //        if (sourceAssembler.Diagnostics.HasErrors)
                //        {
                //            lock (Console.Error)
                //            {
                //                Console.ForegroundColor = ConsoleColor.Red;
                //                Console.Error.WriteLine($"Assembly of '{inputFile}' failed:");
                //                sourceAssembler.Diagnostics.PrintAll(Console.Error);
                //                Console.Error.WriteLine();
                //                Console.ForegroundColor = ConsoleColor.White;
                //            }
                //        }
                //        else
                //        {
                //            var outputFileName = outputFile.FullName;
                //            Print($"Successful compilation, writing '{Path.GetRelativePath(Directory.GetCurrentDirectory(), outputFileName)}'...");
                //            var ysc = new YscFile { Script = sourceAssembler.OutputScript };
                //            var data = ysc.Save(Path.GetFileName(outputFileName));
                //            File.WriteAllBytes(outputFileName, data);

                //            if (options.Unencrypted)
                //            {
                //                data = ysc.Save();
                //                File.WriteAllBytes(Path.ChangeExtension(outputFileName, "unencrypted.ysc"), data);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        lock (Console.Error)
                //        {
                //            Console.ForegroundColor = ConsoleColor.Red;
                //            Console.Error.WriteLine($"Compilation of '{inputFile}' failed:");
                //            d.PrintAll(Console.Error);
                //            Console.Error.WriteLine();
                //            Console.ForegroundColor = ConsoleColor.White;
                //        }
                //    }
                //}
                //catch (Exception e)
                //{
                //    lock (Console.Error)
                //    {
                //        Console.ForegroundColor = ConsoleColor.Red;
                //        Console.Error.Write(e.ToString());
                //        Console.ForegroundColor = ConsoleColor.White;
                //    }
                //    return;
                //}
            });

            totalTime.Stop();
            Print($"Total time: {totalTime.Elapsed}");
        }


        //private sealed class FileUsingResolver : IUsingResolver
        //{
        //    public string NormalizeFilePath(string filePath) => Path.GetFullPath(filePath);

        //    public (Func<TextReader> Open, string FilePath) Resolve(string originPath, string usingPath)
        //    {
        //        var p = NormalizeFilePath(Path.Combine(Path.GetDirectoryName(originPath), usingPath));
        //        return (() => new StreamReader(p), p);
        //    }
        //}

        private static void GenNatives(FileInfo nativeDB)
        {
            var db = NativeDB.FromJson(File.ReadAllText(nativeDB.FullName));
            NativeCommandsGen.Generate(Console.Out, db);
        }

        private class DisassembleOptions
        {
            public FileGlob[] Input { get; set; }
            public DirectoryInfo Output { get; set; }
            public FileInfo NativeDB { get; set; }
        }

        private static void Disassemble(DisassembleOptions options)
        {
            static void Print(string str)
            {
                lock (Console.Out)
                {
                    Console.WriteLine(str);
                }
            }

            NativeDB nativeDB = null;
            if (options.NativeDB != null)
            {
                nativeDB = NativeDB.FromJson(File.ReadAllText(options.NativeDB.FullName));
            }

            Parallel.ForEach(options.Input.SelectMany(i => i.Matches), inputFile =>
            {
                var outputFile = new FileInfo(Path.Combine(options.Output.FullName, Path.ChangeExtension(inputFile.Name, "scasm")));

                Print($"Reading '{inputFile}'...");
                byte[] fileData = File.ReadAllBytes(inputFile.FullName);

                Print($"Loading '{inputFile}'...");
                YscFile ysc = new YscFile();
                ysc.Load(fileData);

                Print($"Disassembling '{inputFile}'...");
                Script sc = ysc.Script;
                const int BufferSize = 1024 * 1024 * 32; // 32mb
                using TextWriter w = new StreamWriter(outputFile.Open(FileMode.Create), Encoding.UTF8, BufferSize) { AutoFlush = false };
                Disassembler.Disassemble(w, sc, nativeDB);
            });
        }

        private class DisassemblerE2EOptions
        {
            public FileGlob[] Input { get; set; }
            public DirectoryInfo Output { get; set; }
            public FileInfo NativeDB { get; set; }
            public bool Debug { get; set; }
        }

        private static void DisassemblerE2E(DisassemblerE2EOptions options)
        {
            static void PrintNoLine(string str)
            {
                lock (Console.Out)
                {
                    Console.Write(str);
                }
            }

            static void Print(string str)
            {
                lock (Console.Out)
                {
                    Console.WriteLine(str);
                }
            }

            static void PrintIf(bool condition, string str)
            {
                if (condition)
                {
                    Print(str);
                }
            }

            NativeDB nativeDB = null;
            if (options.NativeDB != null)
            {
                nativeDB = NativeDB.FromJson(File.ReadAllText(options.NativeDB.FullName));
            }

            var files = options.Input.SelectMany(i => i.Matches).ToArray();
            int count = 0, max = files.Length;

            void OneDone()
            {
                int c = Interlocked.Increment(ref count);
                if (!Console.IsOutputRedirected)
                {
                    PrintNoLine($"\rProgress {c} / {max}");
                }
            }

            var sw = Stopwatch.StartNew();
            var tasks = new List<Task>();
            Parallel.ForEach(options.Input.SelectMany(i => i.Matches), inputFile =>
            {
                var outputDir = options.Output is null ?
                                    null :
                                    options.Output.CreateSubdirectory(Path.GetFileNameWithoutExtension(inputFile.Name)).FullName;
                bool CanOutput() => outputDir is not null;
                void OutputFile(string fileName, string contents)
                {
                    if (CanOutput())
                    {
                        tasks.Add(File.WriteAllTextAsync(Path.Join(outputDir, fileName), contents));
                    }
                }

                var fileData = File.ReadAllBytes(inputFile.FullName);

                var ysc = new YscFile();
                ysc.Load(fileData);
                var originalScript = ysc.Script;
                string originalDisassembly;
                using (var originalDisassemblyWriter = new StringWriter())
                {
                    Disassembler.Disassemble(originalDisassemblyWriter, originalScript, nativeDB);
                    originalDisassembly = originalDisassemblyWriter.ToString();
                }
                if (CanOutput())
                {
                    OutputFile("original_disassembly.txt", originalDisassembly);
                    using var originalDumpWriter = new StringWriter();
                    new Dumper(originalScript).Dump(originalDumpWriter, true, true, true, true, true);
                    OutputFile("original_dump.txt", originalDumpWriter.ToString());
                }

                Assembler reassembled;
                using (var r = new StringReader(originalDisassembly.ToString()))
                {
                    reassembled = Assembler.Assemble(r, Path.ChangeExtension(inputFile.Name, "reassembled.scasm"),
                                                     nativeDB,
                                                     options: new() { IncludeFunctionNames = options.Debug });
                }

                byte[] reassembledData = new YscFile { Script = reassembled.OutputScript }.Save();
                var reassembledYsc = new YscFile();
                reassembledYsc.Load(reassembledData);
                var newScript = reassembledYsc.Script;

                string newDisassembly;
                using (var newDisassemblyWriter = new StringWriter())
                {
                    Disassembler.Disassemble(newDisassemblyWriter, newScript, nativeDB);
                    newDisassembly = newDisassemblyWriter.ToString();
                }

                string S(string msg) => $"{(Console.IsOutputRedirected ? "" : "\r")}[{inputFile}] > {msg}";

                if (reassembled.Diagnostics.HasErrors || reassembled.Diagnostics.HasWarnings)
                {
                    lock (Console.Out)
                    {
                        Console.WriteLine(S("Errors in re-assembly"));
                        reassembled.Diagnostics.PrintAll(Console.Out);
                    }
                }

                PrintIf(originalDisassembly != newDisassembly, S("Disassembly is different"));
                if (CanOutput())
                {
                    OutputFile("new_disassembly.txt", newDisassembly);
                    using var newDumpWriter = new StringWriter();
                    new Dumper(newScript).Dump(newDumpWriter, true, true, true, true, true);
                    OutputFile("new_dump.txt", newDumpWriter.ToString());
                }

                var sc1 = originalScript;
                var sc2 = newScript;
                PrintIf(sc1.Hash != sc2.Hash, S("Hash is different"));
                PrintIf(sc1.Name != sc2.Name, S("Name is different"));
                PrintIf(sc1.NameHash != sc2.NameHash, S("NameHash is different"));
                PrintIf(sc1.NumRefs != sc2.NumRefs, S("NumRefs is different"));

                PrintIf(sc1.CodeLength != sc2.CodeLength, S("CodeLength is different"));
                if (sc1.CodePages != null && sc2.CodePages != null)
                {
                    var codePageIdx = 0;
                    foreach (var (page1, page2) in sc1.CodePages.Zip(sc2.CodePages))
                    {
                        var equal = Enumerable.SequenceEqual(page1.Data, page2.Data);
                        PrintIf(!equal, S($"CodePage #{codePageIdx} is different"));
                        if (!equal)
                        {
                            if (CanOutput())
                            {
                                OutputFile($"original_code_page_{codePageIdx}.txt", string.Join(' ', page1.Data.Select(b => b.ToString("X2"))));
                                OutputFile($"new_code_page_{codePageIdx}.txt", string.Join(' ', page2.Data.Select(b => b.ToString("X2"))));
                            }
                        }
                        codePageIdx++;
                    }
                }

                PrintIf(sc1.StaticsCount != sc2.StaticsCount, S("StaticsCount is different"));
                PrintIf(sc1.ArgsCount != sc2.ArgsCount, S("ArgsCount is different"));
                PrintIf((sc1.Statics != null) != (sc2.Statics != null), S("Statics null"));
                if (sc1.Statics != null && sc2.Statics != null)
                {
                    var staticIdx = 0;
                    foreach (var (static1, static2) in sc1.Statics.Zip(sc2.Statics))
                    {
                        PrintIf(static1.AsUInt64 != static2.AsUInt64, S($"Static #{staticIdx} is different"));
                        staticIdx++;
                    }
                }

                PrintIf(sc1.GlobalsLength != sc2.GlobalsLength, S("GlobalsLength is different"));
                PrintIf(sc1.GlobalsBlock != sc2.GlobalsBlock, S("GlobalsBlock is different"));
                PrintIf((sc1.GlobalsPages != null) != (sc2.GlobalsPages != null), S("Globals null"));
                if (sc1.GlobalsPages != null && sc2.GlobalsPages != null)
                {
                    var globalIdx = 0;
                    foreach (var (page1, page2) in sc1.GlobalsPages.Zip(sc2.GlobalsPages))
                    {
                        foreach (var (global1, global2) in page1.Data.Zip(page2.Data))
                        {
                            PrintIf(global1.AsUInt64 != global2.AsUInt64, S($"Global #{globalIdx} is different"));
                            globalIdx++;
                        }
                    }
                }

                PrintIf(sc1.NativesCount != sc2.NativesCount, S("NativesCount is different"));
                PrintIf((sc1.Natives != null) != (sc2.Natives != null), S("Natives null"));
                if (sc1.Natives != null && sc2.Natives != null)
                {
                    PrintIf(!Enumerable.SequenceEqual(sc1.Natives, sc2.Natives), S("Natives is different"));
                }

                PrintIf(sc1.StringsLength != sc2.StringsLength, S("StringsLength is different"));
                if (sc1.StringsPages != null && sc2.StringsPages != null)
                {
                    var stringPageIdx = 0;
                    foreach (var (page1, page2) in sc1.StringsPages.Zip(sc2.StringsPages))
                    {
                        PrintIf(!Enumerable.SequenceEqual(page1.Data, page2.Data), S($"StringsPage #{stringPageIdx} is different"));
                        stringPageIdx++;
                    }
                }

                OneDone();
            });
            sw.Stop();
            if (!Console.IsOutputRedirected)
            {
                Console.WriteLine();
            }
            Console.WriteLine($"Took {sw.Elapsed}");

            Task.WaitAll(tasks.ToArray());
        }

        private class DumpOptions
        {
            public FileGlob[] Input { get; set; }
            public FileInfo Output { get; set; }
            public bool NoMetadata { get; set; }
            public bool NoDisassembly { get; set; }
            public bool NoBytes { get; set; }
            public bool NoOffsets { get; set; }
            public bool NoInstructions { get; set; }
        }

        private static void Dump(DumpOptions o)
        {
            Parallel.ForEach(o.Input.SelectMany(i => i.Matches), inputFile =>
            {
                var fileData =  File.ReadAllBytes(inputFile.FullName);

                var ysc = new YscFile();
                ysc.Load(fileData);

                var sc = ysc.Script;

                using var w = o.Output switch
                {
                    null => Console.Out,
                    _ => new StreamWriter(Path.Combine(o.Output.FullName, Path.ChangeExtension(inputFile.Name, "txt")))
                };

                new Dumper(sc).Dump(w, showMetadata: !o.NoMetadata, showDisassembly: !o.NoDisassembly,
                                    showOffsets: !o.NoOffsets, showBytes: !o.NoBytes, showInstructions: !o.NoInstructions);
            });
        }

        private class FetchNativeDbOptions
        {
            public Uri SHVUrl { get; set; }
            public Uri NativeDbUrl { get; set; }
            public FileInfo Output { get; set; }
        }

        private static async Task FetchNativeDb(FetchNativeDbOptions o)
        {
            NativeDB db = await NativeDB.Fetch(o.NativeDbUrl, o.SHVUrl);

            await File.WriteAllTextAsync(o.Output.FullName, db.ToJson());
        }
    }
}

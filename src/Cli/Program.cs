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
    using ScTools.ScriptAssembly.CodeGen;
    using ScTools.ScriptAssembly.Disassembly;
    using ScTools.ScriptLang;

    internal static class Program
    {
        private static int Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            var rootCmd = new RootCommand("Tool for working with Grand Theft Auto V script files (.ysc).");

            Command dump = new Command("dump")
            {
                new Argument<FileInfo>("input", "The input YSC file.").ExistingOnly(),
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
            };
            disassemble.Handler = CommandHandler.Create<DisassembleOptions>(Disassemble);

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
                    "The SCNDB file containing the native commands definitions.")
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

            Command fetchNativeDbOld = new Command("fetch-nativedb-old")
            {
                new Option<Uri>(
                    new[] { "--crossmap-url", "-c" },
                    () => new Uri("https://raw.githubusercontent.com/citizenfx/fivem/master/code/components/rage-scripting-five/include/CrossMapping_Universal.h"),
                    "Specifies the URL from which to download the natives cross map."),
                new Option<Uri>(
                    new[] { "--nativedb-url", "-n" },
                    () => new Uri("https://raw.githubusercontent.com/alloc8or/gta5-nativedb-data/master/natives.json"),
                    "Specifies the URL from which to download the native DB data."),
                new Option<FileInfo>(
                    new[] { "--output", "-o" },
                    () => new FileInfo("natives.scndb"),
                    "The output SCNDB file.")
                    .LegalFilePathsOnly(),
            };
            fetchNativeDbOld.Handler = CommandHandler.Create<FetchNativeDbOldOptions>(FetchNativeDbOld);

            Command fetchNativeDb = new Command("fetch-nativedb")
            {
                new Argument<FileInfo>(
                    "shv-zip",
                    "Specifies the path to the ScriptHookV .zip file.")
                    .ExistingOnly(),
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
            rootCmd.AddCommand(assemble);
            rootCmd.AddCommand(compile);
            rootCmd.AddCommand(genNatives);
            rootCmd.AddCommand(fetchNativeDbOld);
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

            NativeDBOld nativeDB;
            using (var reader = new BinaryReader(options.NativeDB.OpenRead()))
            {
                nativeDB = NativeDBOld.Load(reader);
            }

            Parallel.ForEach(options.Input.SelectMany(i => i.Matches), inputFile =>
            {
                var outputFile = new FileInfo(Path.Combine(options.Output.FullName, Path.ChangeExtension(inputFile.Name, "ysc")));

                Print($"Reading '{inputFile}'...");
                string source = File.ReadAllText(inputFile.FullName);

                YscFile ysc = new YscFile();
                try
                {
                    Print($"Assembling '{inputFile}'...");
                    Script sc = Assembler.Assemble(source, nativeDB, new CodeGenOptions(includeFunctionNames: options.FunctionNames));
                    ysc.Script = sc;
                }
                catch (Exception e) // TODO: improve assembler error messages
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

            NativeDB nativeDB = null;
            if (options.NativeDB != null)
            {
                nativeDB = NativeDB.FromJson(File.ReadAllText(options.NativeDB.FullName));
            }

            Parallel.ForEach(options.Input.SelectMany(i => i.Matches), inputFile =>
            {
                var outputFile = new FileInfo(Path.Combine(options.Output.FullName, Path.ChangeExtension(inputFile.Name, "ysc")));

                try
                {
                    Print($"Compiling '{inputFile}'...");
                    var compilationTime = Stopwatch.StartNew();
                    var c = new Compilation
                    {
                        SourceResolver = new DefaultSourceResolver(inputFile.DirectoryName),
                        NativeDB = nativeDB,
                    };

                    using (var source = new StreamReader(inputFile.OpenRead()))
                    {
                        c.SetMainModule(source, inputFile.FullName);
                    }
                    c.PerformPendingAnalysis();

                    var diagnostics = c.GetAllDiagnostics();
                    if (diagnostics.HasErrors)
                    {
                        compilationTime.Stop();
                        lock (Console.Error)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.WriteLine($"Compilation of '{inputFile}' failed (time: {compilationTime.Elapsed}):");
                            diagnostics.PrintAll(Console.Error);
                            Console.Error.WriteLine();
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                    else
                    {
                        c.Compile();
                        compilationTime.Stop();

                        var outputFileName = outputFile.FullName;
                        Print($"Successful compilation, writing '{Path.GetRelativePath(Directory.GetCurrentDirectory(), outputFileName)}'... (time: {compilationTime.Elapsed})");
                        YscFile ysc = new YscFile { Script = c.CompiledScript };
                        byte[] data = ysc.Save(Path.GetFileName(outputFileName));
                        File.WriteAllBytes(outputFileName, data);

                        if (options.Unencrypted)
                        {
                            data = ysc.Save();
                            File.WriteAllBytes(Path.ChangeExtension(outputFileName, "unencrypted.ysc"), data);
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
                    return;
                }
            });

            totalTime.Stop();
            Print($"Total time: {totalTime.Elapsed}");
        }

        private static void GenNatives(FileInfo nativeDB)
        {
            var db = NativeDB.FromJson(File.ReadAllText(nativeDB.FullName));
            NativeCommandsGen.Generate(Console.Out, db);
        }

        private class DisassembleOptions
        {
            public FileGlob[] Input { get; set; }
            public DirectoryInfo Output { get; set; }
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
                var disassembly = Disassembler.Disassemble(sc);

                Print($"Writing '{inputFile}'...");
                const int BufferSize = 1024 * 1024 * 32; // 32mb
                using TextWriter w = new StreamWriter(outputFile.Open(FileMode.Create), Encoding.UTF8, BufferSize) { AutoFlush = false };
                Disassembler.Print(w, sc, disassembly);
            });
        }

        private class DumpOptions
        {
            public FileInfo Input { get; set; }
            public FileInfo Output { get; set; }
            public bool NoMetadata { get; set; }
            public bool NoDisassembly { get; set; }
            public bool NoBytes { get; set; }
            public bool NoOffsets { get; set; }
            public bool NoInstructions { get; set; }
        }

        private static void Dump(DumpOptions o)
        {
            byte[] fileData = File.ReadAllBytes(o.Input.FullName);

            YscFile ysc = new YscFile();
            ysc.Load(fileData);

            Script sc = ysc.Script;

            using TextWriter w = o.Output switch
            {
                null => Console.Out,
                _ => new StreamWriter(o.Output.Open(FileMode.Create))
            };

            new Dumper(sc).Dump(w, showMetadata: !o.NoMetadata, showDisassembly: !o.NoDisassembly,
                                showOffsets: !o.NoOffsets, showBytes: !o.NoBytes, showInstructions: !o.NoInstructions);
        }

        private class FetchNativeDbOldOptions
        {
            public Uri CrossMapUrl { get; set; }
            public Uri NativeDbUrl { get; set; }
            public FileInfo Output { get; set; }
        }

        private static async Task FetchNativeDbOld(FetchNativeDbOldOptions o)
        {
            NativeDBOld db = await NativeDBOld.Fetch(o.CrossMapUrl, o.NativeDbUrl);

            using var w = new BinaryWriter(o.Output.Open(FileMode.Create));
            db.Save(w);

#if DEBUG
            {
                using var buffer = new MemoryStream();
                using var writer = new BinaryWriter(buffer, Encoding.ASCII, leaveOpen: true);
                db.Save(writer);

                buffer.Position = 0;
                using var reader = new BinaryReader(buffer, Encoding.ASCII, leaveOpen: true);
                NativeDBOld copyDB = NativeDBOld.Load(reader);

                Debug.Assert(copyDB.Natives.Length == db.Natives.Length &&
                             copyDB.Natives.Select((n, i) => n == db.Natives[i]).All(b => b),
                             "natives do not match");
            }
#endif
        }

        private class FetchNativeDbOptions
        {
            public FileInfo SHVZip { get; set; }
            public Uri NativeDbUrl { get; set; }
            public FileInfo Output { get; set; }
        }

        private static async Task FetchNativeDb(FetchNativeDbOptions o)
        {
            NativeDB db = await NativeDB.Fetch(o.NativeDbUrl, o.SHVZip.FullName);

            await File.WriteAllTextAsync(o.Output.FullName, db.ToJson());
        }
    }
}

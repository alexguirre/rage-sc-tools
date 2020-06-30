namespace ScTools
{
    using System;
    using System.Text;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using CodeWalker.GameFiles;
    using ScTools.GameFiles;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Linq;

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
                new Argument<FileInfo>(
                    "input",
                    "The input YSC file.")
                    .ExistingOnly(),
                new Option<FileInfo>(
                    new[] { "--output", "-o" },
                    "The output file. If not specified, the output file path is the same as the input file but with the extension changed to '.scasm'.")
                    .LegalFilePathsOnly(),
            };
            disassemble.Handler = CommandHandler.Create<FileInfo, FileInfo>(Disassemble);

            Command assemble = new Command("assemble")
            {
                new Argument<FileInfo>(
                    "input",
                    "The input SCASM file.")
                    .ExistingOnly(),
                new Option<DirectoryInfo>(
                    new[] { "--output", "-o" },
                    () => new DirectoryInfo(".\\"),
                    "The output directory.")
                    .ExistingOnly(),
                new Option(new[] { "--function-names", "-f" }, "Include the function names in ENTER instructions."),
                new Option<FileInfo>(
                    new[] { "--nativedb", "-n" },
                    "The SCNDB file containing the native commands definitions.")
                    .ExistingOnly(),
            };
            assemble.Handler = CommandHandler.Create<AssembleOptions>(Assemble);

            Command fetchNativeDb = new Command("fetch-nativedb")
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
            fetchNativeDb.Handler = CommandHandler.Create<FetchNativeDbOptions>(FetchNativeDb);

            rootCmd.AddCommand(dump);
            rootCmd.AddCommand(disassemble);
            rootCmd.AddCommand(assemble);
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
            public FileInfo Input { get; set; }
            public DirectoryInfo Output { get; set; }
            public bool FunctionNames { get; set; }
            public FileInfo NativeDB { get; set; }
        }

        private static void Assemble(AssembleOptions o)
        {
            LoadGTA5Keys();

            YscFile ysc = new YscFile();

            try
            {
                NativeDB nativeDB = null;
                if (o.NativeDB != null)
                {
                    using var reader = new BinaryReader(o.NativeDB.OpenRead());
                    nativeDB = NativeDB.Load(reader);
                }

                Script sc = new Assembler(new AssemblerOptions(includeFunctionNames: o.FunctionNames),
                                          nativeDB)
                            .Assemble(o.Input);
                ysc.Script = sc;
            }
            catch (AssemblerSyntaxException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write(e.UserMessage);
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }

            string outputPath = Path.Combine(o.Output.FullName, Path.GetFileName(Path.ChangeExtension(o.Input.FullName, "ysc")));
            byte[] data = ysc.Save(Path.GetFileName(outputPath));
            File.WriteAllBytes(outputPath, data);

            outputPath = Path.ChangeExtension(outputPath, "unencrypted.ysc");
            data = ysc.Save();
            File.WriteAllBytes(outputPath, data);
        }

        private static void Disassemble(FileInfo input, FileInfo output)
        {
            output ??= new FileInfo(Path.ChangeExtension(input.FullName, "scasm"));

            byte[] fileData = File.ReadAllBytes(input.FullName);

            YscFile ysc = new YscFile();
            ysc.Load(fileData);

            Script sc = ysc.Script;

            const int BufferSize = 1024 * 1024 * 32; // 32mb
            using TextWriter w = new StreamWriter(output.Open(FileMode.Create), Encoding.UTF8, BufferSize) { AutoFlush = false };
            new Disassembler(sc).Disassemble(w);
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

        private class FetchNativeDbOptions
        {
            public Uri CrossMapUrl { get; set; }
            public Uri NativeDbUrl { get; set; }
            public FileInfo Output { get; set; }
        }

        private static async Task FetchNativeDb(FetchNativeDbOptions o)
        {
            NativeDB db = await NativeDB.Fetch(o.CrossMapUrl, o.NativeDbUrl);
            
            using var w = new BinaryWriter(o.Output.Open(FileMode.Create));
            db.Save(w);

#if DEBUG
            {
                using var buffer = new MemoryStream();
                using var writer = new BinaryWriter(buffer, Encoding.ASCII, leaveOpen: true);
                db.Save(writer);

                buffer.Position = 0;
                using var reader = new BinaryReader(buffer, Encoding.ASCII, leaveOpen: true);
                NativeDB copyDB = NativeDB.Load(reader);

                Debug.Assert(copyDB.Natives.Length == db.Natives.Length &&
                             copyDB.Natives.Select((n, i) => n == db.Natives[i]).All(b => b),
                             "natives do not match");
            }
#endif
        }
    }
}

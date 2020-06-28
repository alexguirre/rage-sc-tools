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
                new Option(new[] { "--function-names", "-f" }, "Include the function names in ENTER instructions."),
            };
            assemble.Handler = CommandHandler.Create<AssembleOptions>(Assemble);


            rootCmd.AddCommand(dump);
            rootCmd.AddCommand(disassemble);
            rootCmd.AddCommand(assemble);

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
            public bool FunctionNames { get; set; }
        }

        private static void Assemble(AssembleOptions o)
        {
            LoadGTA5Keys();

            YscFile ysc = new YscFile();

            Script sc = new Assembler(new AssemblerOptions(includeFunctionNames: o.FunctionNames)).Assemble(o.Input);
            ysc.Script = sc;

            string outputPath = Path.ChangeExtension(o.Input.FullName, "ysc");
            byte[] data = ysc.Save(Path.GetFileName(outputPath));
            File.WriteAllBytes(outputPath, data);

            outputPath = Path.ChangeExtension(o.Input.FullName, "unencrypted.ysc");
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
    }
}

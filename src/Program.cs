namespace ScTools
{
    using System;
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
                new Argument<FileInfo>(
                    "input",
                    "The input YSC file.")
                    .ExistingOnly(),
                new Option<FileInfo>(
                    new[] { "--output", "-o" },
                    "The output file. If not specified, the dump is printed to the console.")
                    .LegalFilePathsOnly(),
            };
            dump.Handler = CommandHandler.Create<FileInfo, FileInfo>(Dump);

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
            };
            assemble.Handler = CommandHandler.Create<FileInfo>(Assemble);


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

        private static void Assemble(FileInfo input)
        {
            LoadGTA5Keys();

            YscFile ysc = new YscFile();

            Script sc = new Assembler().Assemble(input);
            ysc.Script = sc;

            string outputPath = Path.ChangeExtension(input.FullName, "ysc");
            byte[] data = ysc.Save(Path.GetFileName(outputPath));
            File.WriteAllBytes(outputPath, data);

            outputPath = Path.ChangeExtension(input.FullName, "unencrypted.ysc");
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

            using TextWriter w = new StreamWriter(output.Open(FileMode.Create));
            new Disassembler(sc).Disassemble(w);
        }

        private static void Dump(FileInfo input, FileInfo output)
        {
            byte[] fileData = File.ReadAllBytes(input.FullName);

            YscFile ysc = new YscFile();
            ysc.Load(fileData);

            Script sc = ysc.Script;

            using TextWriter w = output switch
            {
                null => Console.Out,
                _ => new StreamWriter(output.Open(FileMode.Create))
            };

            Dump(sc, w);
        }

        private static void Dump(Script sc, TextWriter w)
        {
            w.WriteLine("Name = {0} (0x{1:X8})", sc.Name, sc.NameHash);
            w.WriteLine("Hash = 0x{0:X8}", sc.Hash);
            w.WriteLine("Statics Count = {0}", sc.StaticsCount);
            if (sc.Statics != null)
            {
                int i = 0;
                foreach (ScriptValue v in sc.Statics)
                {
                    w.WriteLine("\t[{0}] = {1:X16} ({2}) ({3})", i++, v.AsInt64, v.AsInt32, v.AsFloat);
                }
            }
            w.WriteLine("Globals Length = {0}", sc.GlobalsLengthAndBlock);
            {
                uint globalBlock = sc.GlobalsLengthAndBlock >> 18;
                w.WriteLine("Globals Block = {0}", globalBlock);

                if (sc.GlobalsPages != null)
                {
                    uint pageIndex = 0;
                    foreach (ScriptValue[] page in sc.GlobalsPages)
                    {
                        uint i = 0;
                        foreach (ScriptValue g in page)
                        {
                            uint globalId = (globalBlock << 18) | (pageIndex << 14) | i;

                            w.WriteLine("\t[{0}] = {1:X16} ({2}) ({3})", globalId, g.AsInt64, g.AsInt32, g.AsFloat);

                            i++;
                        }
                        pageIndex++;
                    }
                }
            }
            w.WriteLine("Natives Count = {0}", sc.NativesCount);
            if (sc.Natives != null)
            {
                int i = 0;
                foreach (ulong hash in sc.Natives)
                {
                    w.WriteLine("\t{0:X16} -> {1:X16}", hash, sc.NativeHash(i));
                    i++;
                }
            }
            w.WriteLine("Code Length = {0}", sc.CodeLength);
            w.WriteLine("Num Refs = {0}", sc.NumRefs);
            w.WriteLine("Strings Length = {0}", sc.StringsLength);
            foreach (uint sid in sc.StringIds())
            {
                w.WriteLine("\t[{0}] = {1}", sid, sc.String(sid));
            }
            w.WriteLine("Disassembly:");
            new Disassembler(sc).Disassemble(w);
        }
    }
}

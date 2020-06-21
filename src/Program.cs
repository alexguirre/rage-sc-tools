namespace ScTools
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Globalization;
    using System.IO;
    using System.Threading;
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

            Command loadandsave = new Command("loadandsave")
            {
                new Argument<FileInfo>(
                    "input",
                    "The input YSC file.")
                    .ExistingOnly(),
            };
            loadandsave.Handler = CommandHandler.Create<FileInfo>(LoadAndSave);

            rootCmd.AddCommand(dump);
            rootCmd.AddCommand(disassemble);
            rootCmd.AddCommand(assemble);
            rootCmd.AddCommand(loadandsave);

            return rootCmd.InvokeAsync(args).Result;
            //return rootCmd.InvokeAsync("loadandsave .\\standard_global_init.ysc").Result;
        }

        private static void LoadAndSave(FileInfo input)
        {
            // CRASH in sub_11FE114

            YscFile ysc = new YscFile();
            ysc.Load(File.ReadAllBytes(input.FullName));
            //Dump(ysc.Script, Console.Out);
            Console.WriteLine("===========================");
            byte[] copy = ysc.Save();
            YscFile copyYsc = new YscFile();
            copyYsc.Load(copy);
            //Dump(ysc.Script, Console.Out);

            File.WriteAllBytes(Path.ChangeExtension(input.FullName, "dup.ysc"), copy);
        }

        private static void Assemble(FileInfo input)
        {
            YscFile ysc = new YscFile();

            Script sc = new Assembler().Assemble(input);
            ysc.Script = sc;

            byte[] data = ysc.Save();
            File.WriteAllBytes(Path.ChangeExtension(input.FullName, "ysc"), data);
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
                foreach (ulong hash in sc.Natives)
                {
                    w.WriteLine("\t{0:X16}", hash);
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

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

            rootCmd.AddCommand(dump);
            rootCmd.AddCommand(disassemble);

            return rootCmd.InvokeAsync(args).Result;
            //return rootCmd.InvokeAsync("dump -o startup.dump.txt .\\startup.ysc").Result;
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
            w.WriteLine("Locals Count = {0}", sc.LocalsCount);
            foreach (ScriptValue v in sc.LocalsInitialValues)
            {
                w.WriteLine("\t{0:X16} ({1}) ({2})", v.AsInt64, v.AsInt32, v.AsFloat);
            }
            w.WriteLine("Globals Count = {0}", sc.GlobalsCount);
            w.WriteLine("Natives Count = {0}", sc.NativesCount);
            foreach (ulong hash in sc.Natives)
            {
                w.WriteLine("\t{0:X16}", hash);
            }
            w.WriteLine("Code Length = {0}", sc.CodeLength);
            w.WriteLine("Num Refs = {0}", sc.NumRefs);
            w.WriteLine("Strings Count = {0}", sc.StringsLength);
            foreach (uint sid in sc.StringIds())
            {
                w.WriteLine("\t[{0}] {1}", sid, sc.String(sid));
            }
            w.WriteLine("Disassembly:");
            new Disassembler(sc).Disassemble(w);
        }
    }
}

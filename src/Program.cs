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

            rootCmd.AddCommand(dump);

            return rootCmd.InvokeAsync(args).Result;
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
                _ => new StreamWriter(output.OpenWrite())
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
            w.WriteLine("Strings Count = {0}", sc.StringsCount);
            w.WriteLine("Disassembly:");
            new Disassembler(sc).Disassemble(w);
        }
    }
}

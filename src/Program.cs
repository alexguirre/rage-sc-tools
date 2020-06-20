namespace ScTools
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using ScTools.GameFiles;

    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            string path = Path.GetFullPath("startup.ysc");

            byte[] fileData = File.ReadAllBytes(path);

            YscFile ysc = new YscFile();
            ysc.Load(fileData);

            Script sc = ysc.Script;
            
            Console.WriteLine("Name = {0} (0x{1:X8})", sc.Name, sc.NameHash);
            Console.WriteLine("Locals Count = {0}", sc.LocalsCount);
            Console.WriteLine("Globals Count = {0}", sc.GlobalsCount);
            Console.WriteLine("Natives Count = {0}", sc.NativesCount);
            Console.WriteLine("Code Length = {0}", sc.CodeLength);
            Console.WriteLine("Num Refs = {0}", sc.NumRefs);
            Console.WriteLine("Strings Count = {0}", sc.StringsCount);
            foreach (ScriptValue v in sc.LocalsInitialValues)
            {
                Console.WriteLine("{0:X16} ({1}) ({2})", v.AsInt64, v.AsInt32, v.AsFloat);
            }
        }
    }
}

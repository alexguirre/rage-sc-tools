namespace ScTools.Playground
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using CodeWalker.GameFiles;

    using ScTools;
    using ScTools.GameFiles;
    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Semantics.Symbols;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            LoadGTA5Keys();
            DoTest();
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

        const string Code = @"
SCRIPT_NAME test_script

NATIVE PROC WAIT(INT ms)
NATIVE FUNC INT GET_GAME_TIMER()
NATIVE PROC BEGIN_TEXT_COMMAND_DISPLAY_TEXT(STRING text)
NATIVE PROC END_TEXT_COMMAND_DISPLAY_TEXT(FLOAT x, FLOAT y, INT p2)
NATIVE PROC ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(STRING text)
NATIVE PROC ADD_TEXT_COMPONENT_INTEGER(INT value)
NATIVE FUNC FLOAT TIMESTEP()
NATIVE FUNC BOOL IS_CONTROL_PRESSED(INT padIndex, INT control)

// TODO: support static variables initializers
INT fib0 // = 0
INT fib1 // = 1
INT curr_index
INT curr_value
INT last_time

PROC MAIN()
    fib0 = 0
    fib1 = 1
    curr_index = 0
    curr_value = 0
    last_time = 0

    last_time = GET_GAME_TIMER()
    WHILE TRUE
        WAIT(0)

        // reload                        context                      context secondary
        IF IS_CONTROL_PRESSED(0, 45) AND IS_CONTROL_PRESSED(0, 51) OR IS_CONTROL_PRESSED(0, 52)
            BEGIN_TEXT_COMMAND_DISPLAY_TEXT(""STRING"")
            ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(""Fibonacci"")
            END_TEXT_COMMAND_DISPLAY_TEXT(0.5, 0.175, 0)

            BEGIN_TEXT_COMMAND_DISPLAY_TEXT(""NUMBER"")
            ADD_TEXT_COMPONENT_INTEGER(curr_value)
            END_TEXT_COMMAND_DISPLAY_TEXT(0.5, 0.25, 0)
        ENDIF

        IF GET_GAME_TIMER() - last_time >= 2000
            curr_value = NEXT_FIB()
            last_time = GET_GAME_TIMER()
        ENDIF

    ENDWHILE
ENDPROC

FUNC INT NEXT_FIB()
    INT result

    IF curr_index < 1
        result = 0
    ELSE
        result = fib0 + fib1
        fib0 = fib1
        fib1 = result
    ENDIF

    curr_index = curr_index + 1
    RETURN result
ENDFUNC
";

        public static void DoTest()
        {
            //NativeDB.Fetch(new Uri("https://raw.githubusercontent.com/alloc8or/gta5-nativedb-data/master/natives.json"), "ScriptHookV_1.0.2060.1.zip")
            //    .ContinueWith(t => File.WriteAllText("nativedb.json", t.Result.ToJson()))
            //    .Wait();

            var nativeDB = NativeDB.FromJson(File.ReadAllText("nativedb.json"));

            using var reader = new StringReader(Code);
            var module = Module.Compile(reader, nativeDB: nativeDB);

            var d = module.Diagnostics;
            var symbols = module.SymbolTable;
            Console.WriteLine($"Errors:   {d.HasErrors} ({d.Errors.Count()})");
            Console.WriteLine($"Warnings: {d.HasWarnings} ({d.Warnings.Count()})");
            foreach (var diagnostic in d.AllDiagnostics)
            {
                diagnostic.Print(Console.Out);
            }

            foreach (var s in symbols.Symbols)
            {
                if (s is TypeSymbol t && t.Type is StructType struc)
                {
                    Console.WriteLine($"  > '{t.Name}' Size = {struc.SizeOf}");
                }
            }

            Console.WriteLine();
            new Dumper(module.CompiledScript).Dump(Console.Out, true, true, true, true, true);

            YscFile ysc = new YscFile
            {
                Script = module.CompiledScript
            };

            string outputPath = "test_script.ysc";
            byte[] data = ysc.Save(Path.GetFileName(outputPath));
            File.WriteAllBytes(outputPath, data);

            outputPath = Path.ChangeExtension(outputPath, "unencrypted.ysc");
            data = ysc.Save();
            File.WriteAllBytes(outputPath, data);
            ;
        }
    }
}

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
NATIVE PROC ADD_TEXT_COMPONENT_FLOAT(FLOAT value, INT decimalPlaces)
NATIVE FUNC FLOAT TIMESTEP()
NATIVE FUNC BOOL IS_CONTROL_PRESSED(INT padIndex, INT control)
NATIVE FUNC FLOAT VMAG(VEC3 v)
NATIVE FUNC FLOAT VMAG2(VEC3 v)
NATIVE FUNC FLOAT VDIST(VEC3 v1, VEC3 v2)
NATIVE FUNC FLOAT VDIST2(VEC3 v1, VEC3 v2)
NATIVE FUNC VEC3 GET_GAMEPLAY_CAM_COORD()
NATIVE PROC DELETE_PED(INT& handle)

STRUCT SPAWNPOINT
    FLOAT heading
    VEC3 position
ENDSTRUCT

PROC MAIN()
    SPAWNPOINT sp = <<45.0, <<1.0, 2.0, 3.0>>>>
    DOUBLE(sp.position)

    VEC3& b = sp.position
    DOUBLE(b)

    WHILE TRUE
        WAIT(0)
        DRAW_FLOAT(0.5, 0.15, sp.heading)
        DRAW_FLOAT(0.5, 0.3,  b.x)
        DRAW_FLOAT(0.5, 0.45, b.y)
        DRAW_FLOAT(0.5, 0.60, b.z)
    ENDWHILE
ENDPROC

PROC DOUBLE(VEC3& v)
    v.x = v.x * 2.0
    v.y = v.y * 2.0
    v.z = v.z * 2.0
ENDPROC

PROC DRAW_FLOAT(FLOAT x, FLOAT y, FLOAT v)
    BEGIN_TEXT_COMMAND_DISPLAY_TEXT(""NUMBER"")
    ADD_TEXT_COMPONENT_FLOAT(v, 2)
    END_TEXT_COMMAND_DISPLAY_TEXT(x, y, 0)
ENDPROC
";

        public static void DoTest()
        {
            //NativeDB.Fetch(new Uri("https://raw.githubusercontent.com/alloc8or/gta5-nativedb-data/master/natives.json"), "ScriptHookV_1.0.2060.1.zip")
            //    .ContinueWith(t => File.WriteAllText("nativedb.json", t.Result.ToJson()))
            //    .Wait();

            var nativeDB = NativeDB.FromJson(File.ReadAllText("nativedb.json"));

            using var reader = new StringReader(Code);
            var module = Module.Compile(reader, nativeDB: nativeDB);
            File.WriteAllText("test_script.ast.txt", module.GetAstDotGraph());

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

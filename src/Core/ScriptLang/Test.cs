namespace ScTools.ScriptLang
{
    using System;
    using System.IO;
    using System.Linq;

    using ScTools.GameFiles;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Semantics.Symbols;

    public static class Test
    {
        //        const string Code = @"
        //SCRIPT_NAME test

        //STRUCT VEC2
        //    FLOAT x
        //    FLOAT y
        //ENDSTRUCT

        //PROTO PROC DRAW_CALLBACK(INT alpha)
        //PROTO FUNC INT GET_ALPHA_CALLBACK()

        //STRUCT CALLBACKS
        //    DRAW_CALLBACK       draw
        //    GET_ALPHA_CALLBACK  getAlpha
        //ENDSTRUCT

        //CALLBACKS myCallbacks// = <<DRAW_OTHER_STUFF, GET_ALPHA_VALUE>>
        //INT someStaticValue
        //FLOAT otherStaticValue

        //PROC MAIN()
        //    VEC2 pos = <<0.5, 0.5>>
        //    pos.y = 0.75
        //    INT a = 10
        //    INT b = 5 + -a * 2

        //    WHILE TRUE
        //        WAIT(0)

        //        DRAW_RECT(pos.x, pos.y, 0.1, 0.1, 255, 0, 0, 255, FALSE)

        //        myCallbacks.draw(myCallbacks.getAlpha())

        //        IF NOT TRUE

        //        ENDIF
        //    ENDWHILE
        //ENDPROC

        //FUNC INT GET_ALPHA_VALUE()
        //    RETURN 200
        //ENDFUNC

        //PROC DRAW_OTHER_STUFF(INT alpha)
        //    DRAW_RECT(0.6, 0.6, 0.2, 0.2, 100, 100, 20, alpha, FALSE)
        //ENDPROC

        //// tmp procs until there are native command definitions
        //PROC DRAW_RECT(FLOAT x, FLOAT y, FLOAT w, FLOAT h, INT r, INT g, INT b, INT a, BOOL unk)
        //ENDPROC
        //PROC WAIT(INT ms)
        //ENDPROC
        //";
        const string Code = @"
SCRIPT_NAME test_script

NATIVE PROC WAIT(INT ms)
NATIVE FUNC INT GET_GAME_TIMER()
NATIVE PROC BEGIN_TEXT_COMMAND_DISPLAY_TEXT(STRING text)
NATIVE PROC END_TEXT_COMMAND_DISPLAY_TEXT(FLOAT x, FLOAT y, INT p2)
NATIVE PROC ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(STRING text)
NATIVE PROC ADD_TEXT_COMPONENT_INTEGER(INT value)
NATIVE FUNC FLOAT TIMESTEP()

BOOL c // = TRUE <-- initializers not supported

PROC MAIN()
    INT a = 10
    INT b = 5
    c = TRUE
    BOOL d = FALSE

    b = ADD(GET_VALUE(), ADD(a, b))

    GET_VALUE()

    FLOAT y = 0.0

    WHILE c
        WAIT(0)

        BEGIN_TEXT_COMMAND_DISPLAY_TEXT(""STRING"")
        ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(""Some\nstring"")
        END_TEXT_COMMAND_DISPLAY_TEXT(0.5, y, 0)

        y = (y + (0.5 * TIMESTEP())) % 1.0

        IF c
            BEGIN_TEXT_COMMAND_DISPLAY_TEXT(""STRING"")
            ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(""The var is TRUE"")
            END_TEXT_COMMAND_DISPLAY_TEXT(0.5, 0.5, 0)
        ELSE
            BEGIN_TEXT_COMMAND_DISPLAY_TEXT(""STRING"")
            ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(""The var is FALSE"")
            END_TEXT_COMMAND_DISPLAY_TEXT(0.5, 0.5, 0)
        ENDIF

        IF d
            BEGIN_TEXT_COMMAND_DISPLAY_TEXT(""STRING"")
            ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(""The other var is TRUE"")
            END_TEXT_COMMAND_DISPLAY_TEXT(0.5, 0.75, 0)
        ENDIF
    ENDWHILE
ENDPROC

FUNC INT ADD(INT a, INT b)
    RETURN a + b
ENDFUNC

FUNC INT GET_VALUE()
    RETURN 4
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

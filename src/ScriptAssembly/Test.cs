namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;
    using Antlr4.Runtime.Tree;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Disassembly;
    using ScTools.ScriptAssembly.Grammar;
    using ScTools.ScriptAssembly.Types;

    public static class Test
    {
        private static IEnumerable<Function> F()
        {
            yield return new Function()
            {
                Name = "func_naked",
                Code = new List<Location> { new Location(0, Opcode.NOP), new Location(1, Opcode.NOP) },
            };

            yield return new Function()
            {
                Name = "func_nonnaked",
                Code = new List<Location> { new Location(0, Opcode.NOP), new Location(1, Opcode.NOP) },
                Naked = false,
            };

            yield return new Function()
            {
                Name = "func_with_args",
                Code = new List<Location> { new Location(0, Opcode.NOP), new Location(1, Opcode.NOP) },
                Naked = false,
                Arguments = new List<Argument>
                { 
                    new Argument { Name = "arg1", Offset = 0, Type = AutoType.Instance },
                    new Argument { Name = "arg2", Offset = 1, Type = AutoType.Instance },
                }
            };

            yield return new Function()
            {
                Name = "func_with_return",
                Code = new List<Location> { new Location(0, Opcode.NOP), new Location(1, Opcode.NOP) },
                Naked = false,
                ReturnType = AutoType.Instance
            };

            yield return new Function()
            {
                Name = "func_with_locals",
                Code = new List<Location> { new Location(0, Opcode.NOP), new Location(1, Opcode.NOP) },
                Naked = false,
                Locals = new List<Local>
                {
                    new Local { Name = "local1", Offset = 0, Type = AutoType.Instance },
                    new Local { Name = "local2", Offset = 1, Type = AutoType.Instance },
                }
            };

            yield return new Function()
            {
                Name = "func_with_all",
                Code = new List<Location> { new Location(0, Opcode.NOP), new Location(1, Opcode.NOP) },
                Naked = false,
                Arguments = new List<Argument>
                {
                    new Argument { Name = "arg1", Offset = 0, Type = AutoType.Instance },
                    new Argument { Name = "arg2", Offset = 1, Type = AutoType.Instance },
                },
                ReturnType = AutoType.Instance,
                Locals = new List<Local>
                {
                    new Local { Name = "local1", Offset = 0, Type = AutoType.Instance },
                    new Local { Name = "local2", Offset = 1, Type = AutoType.Instance },
                }
            };
        }

        public static void DoTest()
        {
            //foreach (var f in F())
            //{
            //    Console.WriteLine(Printer.PrintFunction(f));
            //    Console.WriteLine();
            //}

            //return;

            //YscFile ysc2 = new YscFile();
            //ysc2.Load(File.ReadAllBytes("re_bus_tours.orig.ysc"));

            //var funcs2 = Disassembler.Disassemble(ysc2.Script);

            //using TextWriter wr = new StreamWriter("re_bus_tours.scasm");
            //Disassembler.Print(wr, ysc2.Script, funcs2);


            YscFile ysc2 = new YscFile();
            ysc2.Script = Assembler.Assemble(File.ReadAllText("test_high_level.scasm"));

            string outputPath = "test_high_level.ysc";
            byte[] data = ysc2.Save(Path.GetFileName(outputPath));
            File.WriteAllBytes(outputPath, data);

            outputPath = Path.ChangeExtension(outputPath, "unencrypted.ysc");
            data = ysc2.Save();
            File.WriteAllBytes(outputPath, data);




            YscFile ysc3 = new YscFile();
            ysc3.Load(File.ReadAllBytes("test_high_level.unencrypted.ysc"));

            using TextWriter w = new StreamWriter(new FileInfo("test_high_level.dump.txt").Open(FileMode.Create));

            new Dumper(ysc3.Script).Dump(w, showMetadata: true, showDisassembly: true,
                                showOffsets: true, showBytes: true, showInstructions: true);

            return;

            ////using TextWriter wr = new StreamWriter("funcs_analysis.txt");

            //foreach (var f in Directory.EnumerateFiles(".\\allscripts_with_dumps\\", "*.ysc"))
            //{
            //    //Console.WriteLine(f);

            //    YscFile ysc = new YscFile();
            //    ysc.Load(File.ReadAllBytes(f));


            //    var funcs = new Disassembler(ysc.Script).Disassemble();

            //    //wr.WriteLine("{0} (num: {1}):", f, funcs.Length);
            //    //foreach (var func in funcs)
            //    //{
            //    //    wr.WriteLine("\t{0:000000} - {1:000000}  (num: {2})", func.StartIP, func.EndIP, func.Code.Where(l => l.HasInstruction).Count());
            //    //}

            //    //wr.WriteLine("{0}:", f);
            //    //const uint Range = 10;
            //    //bool writing = false;
            //    //foreach (var func in funcs)
            //    //{
            //    //    for (int i = 0; i < func.Code.Count; i++)
            //    //    {
            //    //        var loc = func.Code[i];

            //    //        if (!writing)
            //    //        {
            //    //            if (loc.IP % 0x4000 >= (0x4000 - Range))
            //    //            {
            //    //                writing = true;
            //    //            }
            //    //        }
            //    //        else
            //    //        {
            //    //            uint v = loc.IP % 0x4000;
            //    //            if (v < (0x4000 - Range) && v >= Range)
            //    //            {
            //    //                writing = false;
            //    //                wr.WriteLine();
            //    //            }
            //    //        }


            //    //        if (writing)
            //    //        {
            //    //            wr.WriteLine("\t{0:000000}: {1}", loc.IP, Instruction.Set[loc.Opcode].Mnemonic);
            //    //        }
            //    //    }
            //    //}
            //    //wr.WriteLine();
            //    //wr.WriteLine();
            //    //wr.WriteLine();
            //}



            //return;

            Script sc = Assembler.Assemble(
@"
            $NAME fibonacci_display
            $STATICS_COUNT 5

            ;$STATIC_INT_INIT 0 0  ; fib0
            ;$STATIC_INT_INIT 1 1  ; fib1
            ;$STATIC_INT_INIT 2 0  ; current fibonnaci index
            ;$STATIC_INT_INIT 3 0  ; current fibonnaci value
            ;$STATIC_INT_INIT 4 0  ; last fibonnaci time
            ;
            ;$NATIVE_DEF 0x4EDE34FBADD967A6 ; void WAIT(int ms)
            ;$NATIVE_DEF 0x6EF0D5178A3B92EF ; void BEGIN_TEXT_COMMAND_DISPLAY_TEXT(const char* text)
            ;$NATIVE_DEF 0xBD217E52410D1B67 ; void END_TEXT_COMMAND_DISPLAY_TEXT(float x, float y, int p2)
            ;$NATIVE_DEF 0x6A8B3CC08A759F44 ; void ADD_TEXT_COMPONENT_INTEGER(int value)
            ;$NATIVE_DEF 0x9B35D07DCD0F0B43 ; int GET_GAME_TIMER()
            ;$NATIVE_DEF 0xA89C789CC9FEF523 ; void ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(const char* text)
            ;
            ;$STRING ""NUMBER""
            ;$STRING ""STRING""
            ;$STRING ""Fibonacci""
            ;
            STRUCT MyStruct BEGIN
                field1: AUTO
                field2: AUTO
                ;field3: AUTO[5]
                field4: Vec3
            END
            ;
            STRUCT Vec3 BEGIN
                ; hello
                x: AUTO
                y: AUTO
                z: AUTO
            END

            ;STRUCT Test BEGIN
            ;    a: StructA
            ;    b: StructB
            ;    c: Test
            ;END

            ;STRUCT Cyclic BEGIN
            ;    f: Cyclic
            ;END

            ;STRUCT DepA BEGIN
            ;    b: DepB
            ;END
            ;
            ;STRUCT DepB BEGIn
            ;    a: DepA
            ;END

            STATICS BEGIN
                myName: AUTO
                someInt: AUTO = 5
                someFloat: AUTO = -10.0
                someStruct: MyStruct
                someOtherName: AUTO[5]
            END

            ARGS BEGIN
                arg1: AUTO
                arg2: AUTO = 2
            END

            FUNC NAKED main BEGIN
                    ENTER 0 2

                    NATIVE 0 1 4
                    STATIC_U8_STORE 4   ; lastTime = GET_GAME_TIMER()

                loop: ; infinite loop
                    PUSH_CONST_U8 0
                    NATIVE 1 0 0        ; WAIT(0)

                    ; draw a string
                    PUSH_CONST_7
                    STRING
                    NATIVE 1 0 1        ; BEGIN_TEXT_COMMAND_DISPLAY_TEXT(""STRING"")

                    PUSH_CONST_U8 14
                    STRING
                    NATIVE 1 0 5        ; ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(""Fibonacci"")

                    PUSH_CONST_F 0.5
                testtest:  PUSH_CONST_F 0.175
                    PUSH_CONST_0
                    NATIVE 3 0 2        ; END_TEXT_COMMAND_DISPLAY_TEXT(0.5, 0.175, 0)

                    ; draw the fibonacci number
                    PUSH_CONST_0
                    STRING
                    NATIVE 1 0 1        ; BEGIN_TEXT_COMMAND_DISPLAY_TEXT(""NUMBER"")

                    STATIC_U8_LOAD 3    ; current fibonacci number
                    NATIVE 1 0 3        ; ADD_TEXT_COMPONENT_INTEGER(currentFibonacci)

                    PUSH_CONST_F 0.5
                    PUSH_CONST_F 0.25
                    PUSH_CONST_0
                    NATIVE 3 0 2        ; END_TEXT_COMMAND_DISPLAY_TEXT(0.5, 0.25, 0)

                    ; check if we need to update the fibonacci number
                    PUSH_CONST_S16 2000
                    NATIVE 0 1 4        ; GET_GAME_TIMER()
                    STATIC_U8_LOAD 4    ; lastTime
                    ISUB
                    ILT_JZ loop     ; if (GET_GAME_TIMER() - lastTime) < 2000)
                                     ; then repeat loop
                                     ; else nextFibonacci and repeat loop
                    CALL nextFibonacciNaked
                    STATIC_U8_STORE 3  ; every 2 seconds, store the next fibonacci number in static 3
                    NATIVE 0 1 4
                    STATIC_U8_STORE 4   ; static4 = GET_GAME_TIMER()

                    J loop
                    LEAVE 0 0
            END

            ;FUNC someFunctionWithArgs (arg1: AUTO, arg2: AUTO)
            ;    local1: AUTO
            ;    local2: AUTO
            ;BEGIN
            ;    PUSH_CONST_0
            ;    PUSH_CONST_1
            ;    DROP
            ;    DROP
            ;END
            ;
            ;FUNC someFunctionWithArgsAndReturn(arg1: AUTO, arg2: AUTO): AUTO
            ;    local1: AUTO
            ;    local2: AUTO
            ;BEGIN
            ;    PUSH_CONST_0
            ;    PUSH_CONST_1
            ;    DROP
            ;    DROP
            ;    PUSH_CONST_2
            ;END
            ;
            ;FUNC someFunctionWithReturnAndLocals: AUTO
            ;    local1: AUTO
            ;    local2: AUTO
            ;BEGIN
            ;    PUSH_CONST_0
            ;    PUSH_CONST_1
            ;    DROP
            ;    DROP
            ;    PUSH_CONST_2
            ;END
            ;
            ;FUNC someFunctionWithReturn: Vec3 BEGIN
            ;    PUSH_CONST_0
            ;    PUSH_CONST_1
            ;    DROP
            ;    DROP
            ;    PUSH_CONST_1
            ;    PUSH_CONST_2
            ;    PUSH_CONST_3
            ;END
            ;
            ;FUNC nextFibonacci: AUTO ; no args, returns a fibonacci number, 1 local for return value
            ;    returnValue: AUTO ; local 2
            ;BEGIN
            ;        PUSH_CONST_1
            ;        STATIC_U8_LOAD 2    ; get current fibonacci index
            ;        IGE_JZ else        ; if (index < 1)
            ;    then:
            ;        PUSH_CONST_0
            ;        LOCAL_U8_STORE 2    ; return 0
            ;        J endif
            ;    else:
            ;        STATIC_U8_LOAD 0    ; fib0
            ;        STATIC_U8_LOAD 1    ; fib1
            ;        IADD
            ;        LOCAL_U8_STORE 2    ; return fib0 + fib1
            ;        STATIC_U8_LOAD 1
            ;        STATIC_U8_STORE 0   ; fib0 = fib1
            ;        LOCAL_U8_LOAD 2
            ;        STATIC_U8_STORE 1   ; fib1 = newFib
            ;    endif:
            ;        STATIC_U8_LOAD 2
            ;        IADD_U8 1
            ;        STATIC_U8_STORE 2   ; index++
            ;        LOCAL_U8_LOAD 2     ; push the return value to the stack
            ;END
            ;
            FUNC NAKED nextFibonacciNaked BEGIN  ; no args, returns a fibonacci number, 1 local for return value
                    ENTER 0 3
                    PUSH_CONST_1
                    STATIC_U8_LOAD 2    ; get current fibonacci index
                    IGE_JZ else        ; if (index < 1)
                then:
                    PUSH_CONST_0
                    LOCAL_U8_STORE 2    ; return 0
                    J endif
                else:
                    STATIC_U8_LOAD 0    ; fib0
                    STATIC_U8_LOAD 1    ; fib1
                    IADD
                    LOCAL_U8_STORE 2    ; return fib0 + fib1
                    STATIC_U8_LOAD 1
                    STATIC_U8_STORE 0   ; fib0 = fib1
                    LOCAL_U8_LOAD 2
                    STATIC_U8_STORE 1   ; fib1 = newFib
                endif:
                    STATIC_U8_LOAD 2
                    IADD_U8 1
                    STATIC_U8_STORE 2   ; index++
                    LOCAL_U8_LOAD 2     ; push the return value to the stack
                    LEAVE 0 1
            END
            ");

            ;

            //using TextWriter w = new StreamWriter(new FileInfo("test.dump.txt").Open(FileMode.Create));

            //new Dumper(sc).Dump(w, showMetadata: true, showDisassembly: true,
            //                    showOffsets: true, showBytes: true, showInstructions: true);

            //var dis = new Disassembler(sc);
            //dis.Disassemble();

            ;
        }
    }
}

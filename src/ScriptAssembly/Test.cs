namespace ScTools.ScriptAssembly
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;
    using Antlr4.Runtime.Tree;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Grammar;

    public static class Test
    {
        public static void DoTest()
        {

            Script sc = Assembler.Assemble(
@"
$NAME fibonacci_display
$STATICS 5

$STATIC_INT_INIT 0 0  ; fib0
$STATIC_INT_INIT 1 1  ; fib1
$STATIC_INT_INIT 2 0  ; current fibonnaci index
$STATIC_INT_INIT 3 0  ; current fibonnaci value
$STATIC_INT_INIT 4 0  ; last fibonnaci time

$NATIVE_DEF 0x4EDE34FBADD967A6 ; void WAIT(int ms)
$NATIVE_DEF 0x6EF0D5178A3B92EF ; void BEGIN_TEXT_COMMAND_DISPLAY_TEXT(const char* text)
$NATIVE_DEF 0xBD217E52410D1B67 ; void END_TEXT_COMMAND_DISPLAY_TEXT(float x, float y, int p2)
$NATIVE_DEF 0x6A8B3CC08A759F44 ; void ADD_TEXT_COMPONENT_INTEGER(int value)
$NATIVE_DEF 0x9B35D07DCD0F0B43 ; int GET_GAME_TIMER()
$NATIVE_DEF 0xA89C789CC9FEF523 ; void ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(const char* text)

$STRING ""NUMBER""
$STRING ""STRING""
$STRING ""Fibonacci""

main:
        ENTER 0 2

        NATIVE 0 1 4
        STATIC_U8_STORE 4   ; lastTime = GET_GAME_TIMER()

    .loop: ; infinite loop
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
        PUSH_CONST_F 0.175
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
        ILT_JZ .loop     ; if (GET_GAME_TIMER() - lastTime) < 2000)
                         ; then repeat loop
                         ; else nextFibonacci and repeat loop
        CALL nextFibonacci
        STATIC_U8_STORE 3  ; every 2 seconds, store the next fibonacci number in static 3
        NATIVE 0 1 4
        STATIC_U8_STORE 4   ; static4 = GET_GAME_TIMER()
        
        J .loop
        LEAVE 0 0

nextFibonacci:  ; no args, 1 local for return value
        ENTER 0 3
        PUSH_CONST_1
        STATIC_U8_LOAD 2    ; get current fibonacci index
        IGE_JZ .else        ; if (index < 1)
    .then:
        PUSH_CONST_0
        LOCAL_U8_STORE 2    ; return 0
        J .end
    .else:
        STATIC_U8_LOAD 0    ; fib0
        STATIC_U8_LOAD 1    ; fib1
        IADD
        LOCAL_U8_STORE 2    ; return fib0 + fib1
        STATIC_U8_LOAD 1
        STATIC_U8_STORE 0   ; fib0 = fib1
        LOCAL_U8_LOAD 2
        STATIC_U8_STORE 1   ; fib1 = newFib
    .end:
        STATIC_U8_LOAD 2
        IADD_U8 1
        STATIC_U8_STORE 2   ; index++
        LOCAL_U8_LOAD 2     ; push the return value to the stack
        LEAVE 0 1
");
            ;
        }
    }
}

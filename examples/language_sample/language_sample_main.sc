SCRIPT_NAME language_sample_main
SCRIPT_HASH `language_sample_main`

USING 'language_sample_shared.sch'

ENUM eState
    STATE_0, STATE_1, STATE_2, STATE_3
    STATE_COUNT
ENDENUM

INT aIntByState[STATE_COUNT] // INT[4]

ENUM eTestEnum
    TEST_ENUM_0 = NULL              // 0
    TEST_ENUM_1 = TEST_ENUM_0 + 1   // 1
    TEST_ENUM_2 = TEST_ENUM_1 + 1   // 2
    TEST_ENUM_3 = TEST_ENUM_2 + 1   // 3
ENDENUM

CONST INT TEST_INT_0 = TEST_ENUM_3 - 1          // 2
CONST INT TEST_INT_1 = TEST_ENUM_2 + TEST_INT_0 // 4
CONST INT TEST_INT_2 = NULL                     // 0

CONST BOOL TEST_BOOL_0 = FALSE
CONST BOOL TEST_BOOL_1 = TRUE AND TRUE OR TEST_BOOL_0   // TRUE
CONST BOOL TEST_BOOL_2 = NULL                           // FALSE

CONST FLOAT TEST_FLOAT_0 = 1.0
CONST FLOAT TEST_FLOAT_1 = TEST_FLOAT_2 + TEST_FLOAT_0 * 2.0    // 6.0
CONST FLOAT TEST_FLOAT_2 = TEST_FLOAT_0 * 4.0                   // 4.0
CONST FLOAT TEST_FLOAT_3 = NULL                                 // 0.0

CONST STRING TEST_STRING_0 = "hello"
CONST STRING TEST_STRING_1 = TEST_STRING_0
CONST STRING TEST_STRING_2 = NULL

// CONST INT TEST_CYCLE_0 = TEST_CYCLE_2
// CONST INT TEST_CYCLE_1 = 5 + TEST_CYCLE_0
// CONST INT TEST_CYCLE_2 = TEST_CYCLE_1

// INT nNonConstant = 5
// CONST INT TEST_NON_CONSTANT = nNonConstant

// CONST VECTOR TEST_INVALID_TYPE = <<TEST_FLOAT_0, TEST_FLOAT_1, TEST_FLOAT_2>>

// STRUCT CYCLE_STRUCT
//     CYCLE_STRUCT_HELPER h
//     CYCLE_STRUCT a[10]
// ENDSTRUCT

// STRUCT CYCLE_STRUCT_HELPER
//     CYCLE_STRUCT c
// ENDSTRUCT

// INT nNonConstantArraySize = 10
// INT nTestArray[nNonConstantArraySize]

// INT nNonConstantValue = 10
// STRUCT STRUCT_WITH_NON_CONST_INITIALIZERS
//     INT n = nNonConstantValue
// ENDSTRUCT

STRUCT MY_STRUCT
    INT a
    FLOAT b
    VECTOR c, d
ENDSTRUCT

eMyValue nValue = MY_VALUE_A
eState nState = STATE_0
MY_STRUCT aItems[MY_CONSTANT]
INT nItemCount = 0, nSomeValue = 5
MY_PROCEDURE_T fnMyProc = DEFAULT_MY_PROCEDURE
BOOL bMyProcChanged = FALSE

CONST INT MY_CONSTANT = 8

CONST INT SIZE_10 = 10

INT arrA[SIZE_10]
INT arrB[20]
INT arrC[5]

INT arr2D[10][20]   // array 10 of array 20 of int

CONST STRING CHILD_SCRIPT_NAME = "language_sample_child"

PROC MAIN()
    g_nTimesMainScriptExecuted += 1

    INCREMENT_ALL(arrA)
    INCREMENT_ALL(arrB)
    INCREMENT_ALL(arrC)

    INCREMENT_ALL_ONLY_SIZE_10(arrA)

    RAW_LOOP(arrA)

    INT i
    REPEAT aItems.length i
        INIT_STRUCT(aItems[i], GET_DEFAULT_A(), GET_DEFAULT_B(), GET_DEFAULT_C(), GET_DEFAULT_D())
        aItems[i].a += F2I(aItems[i].b)
        IF i > 4
            BREAK
        ENDIF
    ENDREPEAT

    MODIFY_STRUCT(aItems[0])

    INT n = 10
    IF n > 15
        INT tmp = 5
        n *= tmp
    ELIF n > 10
        INT tmp = 4
        n *= tmp
    ELIF n > 5
        INT tmp = 3
        n *= tmp
    ELSE
        INT tmp = 2
        n *= tmp
    ENDIF

    WHILE TRUE
        WAIT(0)

        IF IS_CONTROL_PRESSED(0, INPUT_CONTEXT_SECONDARY) AND IS_CONTROL_JUST_PRESSED(0, INPUT_CONTEXT)
            nState += 1
            nState %= STATE_COUNT
        ENDIF

        IF IS_CONTROL_PRESSED(0, INPUT_CONTEXT_SECONDARY) AND IS_CONTROL_JUST_PRESSED(0, INPUT_RELOAD)
            REQUEST_SCRIPT(CHILD_SCRIPT_NAME)
            WHILE NOT HAS_SCRIPT_LOADED(CHILD_SCRIPT_NAME)
                WAIT(0)
            ENDWHILE

            CHILD_ARGS args     // CHILD_ARGS.b is default initialized
            args.a = 10
            args.c = 20
            START_NEW_SCRIPT_WITH_ARGS(CHILD_SCRIPT_NAME, args, SIZE_OF(args), 512)

            SET_SCRIPT_AS_NO_LONGER_NEEDED(CHILD_SCRIPT_NAME)
        ENDIF

        CLEAR_TEMP_BUFFER()

        SWITCH nState
            CASE STATE_0
                fnMyProc("CASE A", 1)
                fnMyProc = WRITE_LINE_TO_TEMP_BUFFER
                bMyProcChanged = TRUE
                BREAK
            CASE STATE_1
                fnMyProc("CASE B", 2)
                BREAK
            CASE STATE_2
                fnMyProc("CASE C", 3)
                BREAK
            CASE STATE_3
                fnMyProc("CASE D", 4)
                BREAK
        ENDSWITCH

        DRAW_TEMP_BUFFER()


        STRING playerModelName
        SWITCH GET_ENTITY_MODEL(PLAYER_PED_ID())
            CASE `player_zero`
                playerModelName = "player_zero"
                BREAK
            CASE `player_one`
                playerModelName = "player_one"
                BREAK
            CASE `player_two`
                playerModelName = "player_two"
                BREAK
            DEFAULT
                playerModelName = "unknown"
                BREAK
        ENDSWITCH

        DRAW_TEXT(playerModelName, 0.01, 0.05)

        VECTOR v1 = << 1.0, 2.0, 3.0 >>
        VECTOR v2 = << 4.0, 5.0, 6.0 >>
        VECTOR v3
        v3 = v1 + v2
        v3 = v1 - v2
        v3 = v1 * v2
        v3 = v1 / v2
        v3 = -v1
        v3 = F2V(42.24)

        INT n1 = 1
        INT n2 = 2
        INT n3
        n3 = n1 + n2
        n3 = n1 - n2
        n3 = n1 * n2
        n3 = n1 / n2
        n3 = n1 % n2
        n3 = -n1
        n3 = n1 | n2
        n3 = n1 & n2
        n3 = n1 ^ n2
        n3 = F2I(42.24)

        FLOAT f1 = 1.0
        FLOAT f2 = 2.0
        FLOAT f3
        f3 = f1 + f2
        f3 = f1 - f2
        f3 = f1 * f2
        f3 = f1 / f2
        f3 = f1 % f2
        f3 = -f1
        f3 = I2F(42)

        BOOL b
        b = n1 < n2
        b = n1 <= n2
        b = n1 > n2
        b = n1 >= n2
        b = n1 == n2
        b = n1 <> n2
        
        b = f1 < f2
        b = f1 <= f2
        b = f1 > f2
        b = f1 >= f2
        b = f1 == f2
        b = f1 <> f2

        INT nullInt = NULL                      // same as = 0
        FLOAT nullFloat = NULL                  // same as = 0
        BOOL nullBool = NULL                    // same as = FALSE
        VECTOR nullVec = <<NULL, NULL, NULL>>   // same as = <<0.0, 0.0, 0.0>>
        ENTITY_INDEX nullEntity = NULL          // sets its handle to 0
        PED_INDEX nullPed = NULL                // sets its handle to 0
        VEHICLE_INDEX nullVeh = NULL            // sets its handle to 0

        INT test1 = 1, test2 = 1 + test1

        FLOAT &ref1 = f1, &ref2 = f2
        ref1 = 10.0
        ref1 = f1
        ref1 = ref2
    ENDWHILE
ENDPROC

PROC MODIFY_STRUCT(MY_STRUCT& s)
    s.a = 42
    s.b = 123.45
    s.c = << 1.0, 2.0, 3.0 >>
    s.d = << 4.0, 5.0, 6.0 >>
ENDPROC

PROC INIT_STRUCT(MY_STRUCT& s, INT a, FLOAT b, VECTOR c, VECTOR d)
    s.a = a
    s.b = b
    s.c = c
    s.d = d
ENDPROC

FUNC INT GET_DEFAULT_A()
    RETURN 0
ENDFUNC

FUNC FLOAT GET_DEFAULT_B()
    RETURN 0.0
ENDFUNC

FUNC VECTOR GET_DEFAULT_C()
    RETURN << 0.0, 0.0, 0.0 >>
ENDFUNC

FUNC VECTOR GET_DEFAULT_D()
    RETURN << 1.0, 1.0, 1.0 >>
ENDFUNC

TEXT_LABEL248 lblTempBuffer

PROC CLEAR_TEMP_BUFFER()
    ASSIGN_STRING(lblTempBuffer, "")
ENDPROC

PROC DRAW_TEMP_BUFFER()
    DRAW_TEXT(lblTempBuffer, 0.01, 0.01)
ENDPROC

PROC WRITE_LINE_TO_TEMP_BUFFER(STRING s, INT n)
    APPEND_STRING(lblTempBuffer, s)
    APPEND_STRING(lblTempBuffer, " - ~r~")
    APPEND_INT(lblTempBuffer, n)
    APPEND_STRING(lblTempBuffer, "~s~~n~")
ENDPROC

PROC INCREMENT_ALL(INT arr[])
    INT i
    REPEAT arr.length i
        arr[i] += 1
    ENDREPEAT
ENDPROC

PROC INCREMENT_ALL_ONLY_SIZE_10(INT (&arr)[SIZE_10])
    INT i
    REPEAT arr.length i
        arr[i] += 1
    ENDREPEAT
ENDPROC

PROC RAW_LOOP(INT arr[])

    INT i = 0
begin:
    IF i >= arr.length
        GOTO exit
    ENDIF

    arr[i] *= 2

    i += 1
    GOTO begin
exit:

ENDPROC
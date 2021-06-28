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

// PROC PROC_WITH_RETURN_EXPRESSION() 
//     RETURN 10
// ENDPROC

// FUNC INT FUNC_WITHOUT_RETURN_EXPRESSION() 
//     RETURN
// ENDFUNC

// FUNC INT FUNC_WITH_INCORRECT_TYPE_IN_RETURN_EXPRESSION() 
//     RETURN "hello"
// ENDFUNC

// STRUCT STRUCT_WITH_REPEATED_FIELD_NAMED
//     INT n
//     INT n
//     INT a, a
// ENDSTRUCT

// INT nStaticValue = 5
// INT& staticRef = nStaticValue

STRUCT MY_STRUCT
    INT a
    FLOAT b
    VECTOR c, d
ENDSTRUCT

eMyValue nValue = MY_VALUE_A
eState nState = STATE_0
MY_STRUCT aItems[MY_CONSTANT]
INT nItemCount = 0, nSomeValue = 5
MY_PROCEDURE_T fnMyProc
BOOL bMyProcChanged = FALSE
VECTOR vTest = <<1.0, 2.0, 3.0>> * <<2.0, 4.0, 6.0>>
PED_INDEX hMyPed = NULL

CONST INT MY_CONSTANT = 8

CONST INT SIZE_10 = 10

INT arrA[SIZE_10]
INT arrB[20]
INT arrC[5]

INT arr2D[10][20]   // array 10 of array 20 of int

CONST STRING CHILD_SCRIPT_NAME = "language_sample_child"

PROC MAIN()
    g_nTimesMainScriptExecuted += 1

    fnMyProc = DEFAULT_MY_PROCEDURE

    INCREMENT_ALL(arrA)
    INCREMENT_ALL(arrB)
    INCREMENT_ALL(arrC)

    INCREMENT_ALL_ONLY_SIZE_10(arrA)

    RAW_LOOP(arrA)

    MY_STRUCT tmp
    tmp.a = 1
    tmp.b = 2.0
    tmp.c = <<3.0, 4.0, 5.0>>
    tmp.d = <<6.0, 7.0, 8.0>>
    // tmp.unk = 120

    INT i
    REPEAT aItems.length i
        INIT_STRUCT(aItems[i], GET_DEFAULT_A(), GET_DEFAULT_B(), GET_DEFAULT_C(), GET_DEFAULT_D())

        IF i % 2 == 0
            CONTINUE
        ENDIF

        aItems[i].a += F2I(aItems[i].b)

        IF i > 6
            BREAK
        ENDIF
    ENDREPEAT

    // CONTINUE

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

        // FLOAT &ref1 = f1, &ref2 = f2
        // ref1 = 10.0
        // ref1 = f1
        // ref1 = ref2
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

TEXT_LABEL_247 lblTempBuffer

PROC CLEAR_TEMP_BUFFER()
    lblTempBuffer = ""
ENDPROC

PROC DRAW_TEMP_BUFFER()
    DRAW_TEXT(lblTempBuffer, 0.01, 0.01)
ENDPROC

PROC WRITE_LINE_TO_TEMP_BUFFER(STRING s, INT n)
    APPEND(lblTempBuffer, s)
    APPEND(lblTempBuffer, " - ~r~")
    APPEND(lblTempBuffer, n)
    APPEND(lblTempBuffer, "~s~~n~")
ENDPROC

PROC INCREMENT_ALL(INT arr[])
    INT i
    REPEAT arr.length i
        arr[i] += 1
    ENDREPEAT
ENDPROC

PROC INCREMENT_ALL_ONLY_SIZE_10(INT arr[SIZE_10]) // arrays are passed by reference
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

PROC TEST_HANDLES()
    PED_INDEX ped = NULL
    VEHICLE_INDEX veh = NULL
    OBJECT_INDEX obj = NULL
    ENTITY_INDEX ent = NULL
    PLAYER_INDEX player = NULL

    ent = ped
    ent = veh
    ent = obj
    // ent = player
ENDPROC

// PROC TEST_REFS()
//     FLOAT f1 = 1.0, f2 = 2.0
//     INT i = 1

//     FLOAT& ref1 = f1
//     FLOAT& ref2 = f2
//     // FLOAT& ref3 = i
//     FLOAT& ref4 = ref1

//     ref4 = ref2 + ref1

//     INT& iref = i

//     SWITCH iref
//         CASE 1
//             BREAK
//     ENDSWITCH
// ENDPROC

// STRUCT STRUCT_WITH_REF
//     INT& intRef
// ENDSTRUCT

// INT& arrOfRefs[10]

PROC TEST_ARRAY_PARAMS()
    INT a[10]
    INT b[5][10]
    // INT c[]

    TEST_ARRAY_PARAMS_1(a)
    TEST_2D_ARRAY_PARAMS_1(b)
ENDPROC

PROC TEST_ARRAY_PARAMS_1(INT arrParam[10])
    TEST_ARRAY_PARAMS_2(arrParam)
ENDPROC

PROC TEST_ARRAY_PARAMS_2(INT arrParam[10])
    TEST_ARRAY_PARAMS_3(arrParam)
ENDPROC

PROC TEST_ARRAY_PARAMS_3(INT arrParam[])
ENDPROC

PROC TEST_2D_ARRAY_PARAMS_1(INT arrParam[5][10])
    TEST_2D_ARRAY_PARAMS_2(arrParam)
ENDPROC

PROC TEST_2D_ARRAY_PARAMS_2(INT arrParam[5][10])
    TEST_2D_ARRAY_PARAMS_3(arrParam)
ENDPROC

PROC TEST_2D_ARRAY_PARAMS_3(INT arrParam[][10])
ENDPROC

PROC TEST_2D_ARRAY_PARAMS_4(INT arrParam[][10][20])
ENDPROC

// PROC TEST_INVALID_ARRAY_PARAMS_1(INT arrParam[][])
// ENDPROC

// PROC TEST_INVALID_ARRAY_PARAMS_2(INT arrParam[10][])
// ENDPROC

// PROC TEST_INVALID_ARRAY_PARAMS_3(INT arrParam[10][][5])
// ENDPROC

PROC TEST_ANY()
    ANY any1 = 1
    ANY any2 = 2.0
    ANY any3 = TRUE
    ANY any4 = TEST_ANY
    // ANY any5 = <<1.0, 2.0, 3.0>>
    // ANY& any6 = <<1.0, 2.0, 3.0>>

    VECTOR v = <<1.0, 2.0, 3.0>>
    // ANY& any7 = v

    // any7 = v
    // any7 = 1234
ENDPROC

PROC TEST_ASSIGNMENT()
    // 1 = 2
    // 1 += 2

    VECTOR v1 = <<1.0, 2.0, 3.0>>, v2 = <<4.0, 5.0, 6.0>>
    v1 = v2
    v1 = v1 + v2
    v1 = v1 - v2
    v1 = v1 * v2
    v1 = v1 / v2
    v1 += v2
    v1 -= v2
    v1 *= v2
    v1 /= v2
    // v1 &= v2

    INT a = 1, b = 2
    a = b
    a = a + b
    a = a - b
    a = a * b
    a = a / b
    a = a & b
    a += b
    a -= b
    a *= b
    a /= b
    a &= b
ENDPROC

PROC TEST_SWITCH()
    INT n = 10
    SWITCH n
        CASE 1
            BREAK
        CASE 1 + 1
            BREAK
        CASE 1 * 3
            BREAK
        // CASE 2 + n
        //     BREAK
        // CASE "hello"
        //     BREAK
        // CASE 2
        //     BREAK
        DEFAULT
            BREAK
        // DEFAULT
        //     BREAK
    ENDSWITCH
ENDPROC

PROTO FUNC BOOL TEST_FUNC1_T(INT a, eInput b)
PROTO FUNC INT TEST_FUNC2_T(FLOAT a)

TEST_FUNC2_T staticFnFunc1
// TEST_FUNC2_T staticFnFunc2 = F2I_WRAPPER

PROC TEST_FUNCTION_POINTERS()
    // TEST_FUNC1_T fnFunc1 = IS_CONTROL_JUST_PRESSED
    // TEST_FUNC2_T fnFunc2 = F2I
    TEST_FUNC2_T fnFunc3 = F2I_WRAPPER
    // TEST_FUNC1_T fnFunc4 = F2I_WRAPPER
    // TEST_FUNC1_T fnFunc5 = ASSIGN

    staticFnFunc1 = F2I_WRAPPER

    INT n = fnFunc3(1234.56)
    INT n2 = staticFnFunc1(6789.1234)
ENDPROC

FUNC INT F2I_WRAPPER(FLOAT v)
    RETURN F2I(v)
ENDFUNC

TEXT_LABEL_7 tlStatic1 = "hello world"
TEXT_LABEL_7 tlStatic2 = 1234

PROC TEST_TEXT_LABELS()
    TEXT_LABEL_7 tl1
    TEXT_LABEL_7 tl2
    TEXT_LABEL_63 tl3
    TEXT_LABEL_23 tl4

    tl1 = tlStatic1
    tl2 = tlStatic2

    tl1 = 1
    tl1 = "hello"

    tl2 = tl1
    APPEND(tl1, 1)
    APPEND(tl2, 2)

    tl3 = tl1
    tl4 = CREATE_TEXT_LABEL()

    // DRAW_TEXT(STRING s, FLOAT x, FLOAT y)
    DRAW_TEXT(tl1, 0.5, 0.1)
    DRAW_TEXT(tl2, 0.5, 0.2)
    DRAW_TEXT(tl3, 0.5, 0.3)
    DRAW_TEXT(tl4, 0.5, 0.3)
ENDPROC

FUNC TEXT_LABEL_31 CREATE_TEXT_LABEL()
    TEXT_LABEL_31 tlResult = "hello"
    APPEND(tlResult, 1)
    APPEND(tlResult, 2)
    APPEND(tlResult, 3)
    APPEND(tlResult, "-")
    APPEND(tlResult, 4)
    APPEND(tlResult, 5)
    APPEND(tlResult, 6)
    RETURN tlResult
ENDFUNC
NATIVE PLAYER_INDEX
NATIVE ENTITY_INDEX
NATIVE PED_INDEX : ENTITY_INDEX

NATIVE PROC WAIT(INT timeMs)
NATIVE FUNC BOOL IS_EXITFLAG_SET()
NATIVE PROC SET_EXITFLAG_RESPONSE()
NATIVE PROC SET_STREAMING(BOOL toggle)
NATIVE FUNC PLAYER_INDEX CREATE_PLAYER(INT unk0, VECTOR pos)
NATIVE FUNC PLAYER_INDEX GET_PLAYER_ID()
NATIVE FUNC PED_INDEX GET_PLAYER_PED(PLAYER_INDEX player)
NATIVE FUNC BOOL DOES_MAIN_PLAYER_EXIST()
NATIVE FUNC BOOL IS_PED_DEAD(PED_INDEX ped)
NATIVE PROC SET_PED_COORDS(PED_INDEX ped, VECTOR position, BOOL unk)
NATIVE PROC SET_PED_HEADING(PED_INDEX ped, FLOAT heading)
NATIVE PROC SET_LOAD_COLLISION_FOR_PED_FLAG(PED_INDEX ped, BOOL toggle)
NATIVE PROC REQUEST_COLLISION_AT_COORD(VECTOR position)
NATIVE PROC FREEZE_PED_POSITION(PED_INDEX ped, BOOL toggle)
NATIVE PROC STREAM_HELPERS_INIT()
NATIVE PROC LOAD_SCENE(VECTOR position)

FUNC PLAYER_INDEX PLAYER_ID()
    RETURN GET_PLAYER_ID()
ENDFUNC

FUNC PED_INDEX PLAYER_PED_ID()
    IF DOES_MAIN_PLAYER_EXIST()
        RETURN GET_PLAYER_PED(PLAYER_ID())
    ENDIF

    RETURN NULL
ENDFUNC

PROC TELEPORT_PED(PED_INDEX ped, VECTOR position, FLOAT heading)
    IF NOT IS_PED_DEAD(ped)
        SET_PED_COORDS(ped, position, TRUE)
        SET_PED_HEADING(ped, heading)
    ENDIF
ENDPROC

GLOBALS startup 0
ENDGLOBALS

NATIVE PLAYER_INDEX
NATIVE ENTITY_INDEX
NATIVE PED_INDEX : ENTITY_INDEX

NATIVE PROC GET_PLAYER_CHAR(PLAYER_INDEX player, PED_INDEX &out_ped) = "0x511454A9"
NATIVE FUNC PLAYER_INDEX CONVERT_INT_TO_PLAYERINDEX(INT playerID) = "0x5996315E"
NATIVE FUNC INT GET_PLAYER_ID() = "0x62E319C6"
NATIVE PROC CREATE_PLAYER(INT modelHash, VECTOR pos, PLAYER_INDEX &out_player) = "0x335E3951"
NATIVE PROC SET_CHAR_COORDINATES(PED_INDEX ped, VECTOR pos) = "0x689D0F5F"
NATIVE PROC SET_CHAR_COORDINATES_NO_OFFSET(PED_INDEX ped, VECTOR pos) = "0x57C758F0"
NATIVE PROC SET_CHAR_HEADING(PED_INDEX ped, FLOAT heading) = "0x46B5523B"
NATIVE PROC SET_PLAYER_CONTROL(PLAYER_INDEX player, BOOL toggle) = "0x1A6203EA"
NATIVE PROC WAIT(INT timeMs) = "0x266716AC"
NATIVE PROC FORCE_LOADING_SCREEN(BOOL toggle) = "0x4E68316C"
NATIVE PROC DRAW_RECT(FLOAT x, FLOAT y, FLOAT width, FLOAT height, INT r, INT g, INT b, INT a) = "0x3B2526E3"
NATIVE FUNC BOOL IS_SCREEN_FADED_IN() = "0x5E0713B2"
NATIVE PROC DO_SCREEN_FADE_IN(INT timeMs) = "0x04D72200"
NATIVE PROC DEBUG_OFF() = "0x67177EEC"
NATIVE PROC REQUEST_SCRIPT(STRING scriptName) = "0x6FFE0DFD"
NATIVE FUNC BOOL HAS_SCRIPT_LOADED(STRING scriptName) = "0x2A171915"
NATIVE FUNC INT START_NEW_SCRIPT(STRING scriptName, INT stackSize) = "0x4E2260B9"
NATIVE PROC MARK_SCRIPT_AS_NO_LONGER_NEEDED(STRING scriptName) = "0x09E405DB"
NATIVE PROC TERMINATE_THIS_SCRIPT() = "0x2BCD1ECA"
NATIVE PROC THIS_SCRIPT_SHOULD_BE_SAVED() = "0x48573CF7"
NATIVE PROC PRINT_STRING_WITH_LITERAL_STRING_NOW(STRING str, STRING message, INT timeMs, BOOL unk1) = "0x0CA539D6"
NATIVE PROC ADD_HOSPITAL_RESTART(VECTOR pos, FLOAT heading, INT id) = "0x2AB06643"
NATIVE PROC ADD_POLICE_RESTART(VECTOR pos, FLOAT heading, INT id) = "0x42492860"
NATIVE PROC LOAD_SCENE(VECTOR pos) = "0x39F62BFB"
NATIVE PROC SET_THIS_SCRIPT_CAN_REMOVE_BLIPS_CREATED_BY_ANY_SCRIPT(BOOL toggle) = "0x29D64E72"
NATIVE PROC DO_SCREEN_FADE_IN_UNHACKED(INT timeMs) = "0x5F9218C3"
NATIVE PROC RELEASE_WEATHER() = "0x3A115D9D"
NATIVE PROC RELEASE_TIME_OF_DAY() = "0x2AD2206E"
NATIVE PROC SET_CAR_DENSITY_MULTIPLIER(FLOAT multiplier) = "0x0AA73A12"
NATIVE PROC SET_MAX_WANTED_LEVEL(INT maxLevel) = "0x5D622498"


FUNC PLAYER_INDEX GET_PLAYER_INDEX()
    RETURN CONVERT_INT_TO_PLAYERINDEX(GET_PLAYER_ID())
ENDFUNC

FUNC PED_INDEX GET_PLAYER_PED()
    PED_INDEX result
    GET_PLAYER_CHAR(CONVERT_INT_TO_PLAYERINDEX(GET_PLAYER_ID()), result)
    RETURN result
ENDFUNC


GLOBALS startup 0
#if IS_DEBUG_BUILD
    INT g_nColorR = 255, g_nColorG = 0, g_nColorB = 100, g_nColorA = 200
#endif
#if NOT IS_DEBUG_BUILD
    INT g_nColorR = 0, g_nColorG = 255, g_nColorB = 100, g_nColorA = 200
#endif
    //INT g_buffer[65055-1-4]
ENDGLOBALS

USING 'natives.sch'

ENUM SCRIPT_STATE
    SCRIPT_STATE_WAITING_FOR_ASSETS,
    SCRIPT_STATE_VENDING_MACHINE_READY,
    SCRIPT_STATE_VENDING_MACHINE_EMPTY
ENDENUM

ENUM VENDING_STATE
    VENDING_STATE_PLAYER_OUT_OF_RANGE,
    VENDING_STATE_WAIT_FOR_PLAYER,
    VENDING_STATE_GRAB_PLAYER,
    VENDING_STATE_3, // in original script it is waiting for the player to remove his helmet, we don't check for that in this script
    VENDING_STATE_RUN_VEND,
    VENDING_STATE_RESET_VEND
ENDENUM

ENUM VENDING_MACHINE_TYPE
    VENDING_MACHINE_UNKNOWN,
    VENDING_MACHINE_SODA_01,
    VENDING_MACHINE_SODA_02,
    VENDING_MACHINE_SNACK
ENDENUM

OBJECT_INDEX objVendingMachine
VENDING_MACHINE_TYPE eVendType = VENDING_MACHINE_UNKNOWN
SCRIPT_STATE eState
VENDING_STATE eVendingState
STRING sAnimDict = NULL
VECTOR vVendInRangePoint
VECTOR vVend_78
VECTOR vVend_81
BOOL bIsPlayerControlDisabled = FALSE
OBJECT_INDEX objSodaCan = NULL
INT iNumberOfSodaCansSold = 0

CONST_INT k_iMaxNumberOfSodaCans 10



FUNC BOOL IS_PLAYER_AVAILABLE(BOOL bNotAvailableIfDead, BOOL bOnlyInVehicle, BOOL bDriverOnly)

    IF IS_MINIGAME_IN_PROGRESS()
        RETURN FALSE
    ENDIF

    IF bNotAvailableIfDead
        IF IS_ENTITY_DEAD(PLAYER_PED_ID(), FALSE)
            RETURN FALSE
        ENDIF
    ENDIF

    IF IS_PED_IN_ANY_VEHICLE(PLAYER_PED_ID(), FALSE)
        IF NOT IS_PED_SITTING_IN_ANY_VEHICLE(PLAYER_PED_ID())
            RETURN FALSE
        ENDIF

        VEHICLE_INDEX vehPlayer = GET_VEHICLE_PED_IS_IN(PLAYER_PED_ID(), FALSE)

        IF bNotAvailableIfDead
            IF IS_ENTITY_DEAD(vehPlayer, FALSE)
                RETURN FALSE
            ENDIF
        ENDIF

        IF bDriverOnly
            IF NOT IS_ENTITY_DEAD(vehPlayer, FALSE)
                IF GET_PED_IN_VEHICLE_SEAT(vehPlayer, -1, FALSE) <> PLAYER_PED_ID()
                    RETURN FALSE
                ENDIF
            ENDIF
        ENDIF

        IF NOT IS_ENTITY_DEAD(vehPlayer, FALSE)
            IF GET_ENTITY_UPRIGHT_VALUE(vehPlayer) < 0.95 OR GET_ENTITY_UPRIGHT_VALUE(vehPlayer) > 1.011
                RETURN FALSE
            ENDIF
        ENDIF
    ELIF bOnlyInVehicle
        RETURN FALSE
    ENDIF

    IF NOT IS_PLAYER_READY_FOR_CUTSCENE(PLAYER_ID())
        RETURN FALSE
    ENDIF

    IF NOT CAN_PLAYER_START_MISSION(PLAYER_ID())
        RETURN FALSE
    ENDIF

    RETURN TRUE
ENDFUNC

FUNC BOOL HAS_PLAYER_BEEN_DAMAGED()
    IF HAS_ENTITY_BEEN_DAMAGED_BY_ANY_OBJECT(PLAYER_PED_ID()) OR HAS_ENTITY_BEEN_DAMAGED_BY_ANY_PED(PLAYER_PED_ID()) OR HAS_ENTITY_BEEN_DAMAGED_BY_ANY_VEHICLE(PLAYER_PED_ID()) OR HAS_ENTITY_BEEN_DAMAGED_BY_WEAPON(PLAYER_PED_ID(), HASH("weapon_smokegrenade"), 0)
        CLEAR_ENTITY_LAST_DAMAGE_ENTITY(PLAYER_PED_ID())
        RETURN TRUE
    ENDIF
    RETURN FALSE
ENDFUNC

FUNC BOOL HAS_MONEY()
    RETURN TRUE
ENDFUNC



FUNC VENDING_MACHINE_TYPE GET_VENDING_MACHINE_TYPE(OBJECT_INDEX objVendingMachine)
    IF DOES_ENTITY_EXIST(objVendingMachine)
        SWITCH GET_ENTITY_MODEL(objVendingMachine)

            CASE HASH("prop_vend_soda_01")
                RETURN VENDING_MACHINE_SODA_01

            CASE HASH("prop_vend_soda_02")
                RETURN VENDING_MACHINE_SODA_02

            CASE HASH("prop_vend_snak_01")
                RETURN VENDING_MACHINE_SNACK

        ENDSWITCH
    ENDIF

    RETURN VENDING_MACHINE_UNKNOWN
ENDFUNC

PROC RUN_SCRIPT_STATE_MACHINE()
    SWITCH eState
        CASE SCRIPT_STATE_WAITING_FOR_ASSETS
            IF ARE_ASSETS_READY()
                eState = SCRIPT_STATE_VENDING_MACHINE_READY
                eVendingState = VENDING_STATE_PLAYER_OUT_OF_RANGE
            ENDIF
            BREAK

        CASE SCRIPT_STATE_VENDING_MACHINE_READY
            vVendInRangePoint = GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS(objVendingMachine, << 0.0, -0.97, 0.05 >>)
            vVend_81 = << 0.6, 0.6, 1.0 >>
            vVend_78 = GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS(objVendingMachine, << 0.0, -0.97, 0.05 >>)
            IF NOT IS_PED_INJURED(PLAYER_PED_ID()) AND NOT IS_PED_RAGDOLL(PLAYER_PED_ID()) AND NOT HAS_PLAYER_BEEN_DAMAGED()
                RUN_VENDING_STATE_MACHINE()
            ENDIF
            BREAK

        CASE SCRIPT_STATE_VENDING_MACHINE_EMPTY
            DISPLAY_HELP("VENDEMP") // Vending machine has run out of sodas.
            SET_ENTITY_HEALTH(objVendingMachine, 0, 0) // kill the vending machine to stop the script
            BREAK
    ENDSWITCH
ENDPROC

PROC RUN_VENDING_STATE_MACHINE()

    SWITCH eVendingState
        CASE VENDING_STATE_PLAYER_OUT_OF_RANGE
            IF NOT IS_PED_IN_ANY_VEHICLE(PLAYER_PED_ID(), FALSE)
                IF IS_ENTITY_AT_COORD(PLAYER_PED_ID(), vVendInRangePoint, <<3.2, 3.2, 3.2>>, FALSE, TRUE, 0)
                    IF GET_INTERIOR_FROM_ENTITY(PLAYER_PED_ID()) == GET_INTERIOR_FROM_ENTITY(objVendingMachine)

                        eVendingState = VENDING_STATE_WAIT_FOR_PLAYER
                    ENDIF
                ENDIF
            ENDIF
            BREAK

        CASE VENDING_STATE_WAIT_FOR_PLAYER
            IF IS_PLAYER_AVAILABLE(TRUE, FALSE, TRUE)
                IF IS_PLAYER_PLAYING(PLAYER_ID())
                    IF NOT (IS_PLAYER_FREE_AIMING(PLAYER_ID()) AND IS_PED_ARMED(PLAYER_PED_ID(), 6)) AND NOT IS_PLAYER_TARGETTING_ANYTHING(PLAYER_ID()) AND NOT IS_PED_IN_ANY_VEHICLE(PLAYER_PED_ID(), TRUE) AND IS_ENTITY_AT_COORD(PLAYER_PED_ID(), vVend_78, vVend_81, FALSE, TRUE, 0) AND NOT IS_ENTITY_PLAYING_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT2", 3) AND NOT HAS_PLAYER_BEEN_DAMAGED()
                        IF HAS_MONEY()
                            IF NOT IS_PAUSE_MENU_ACTIVE()
                                DISPLAY_HELP("VENDHLP") // Press ~INPUT_CONTEXT~ to buy a soda for $1.

                                IF IS_CONTROL_JUST_PRESSED(0, INPUT_CONTEXT)
                                    CLEAR_AREA_OF_PROJECTILES(vVendInRangePoint, 3.0, 0)
                                    SET_PLAYER_CONTROL(PLAYER_ID(), FALSE, 256)
                                    SET_PED_CURRENT_WEAPON_VISIBLE(PLAYER_PED_ID(), FALSE, TRUE, TRUE, FALSE)
                                    REQUEST_AMBIENT_AUDIO_BANK("VENDING_MACHINE", FALSE, -1)
                                    SET_PED_STEALTH_MOVEMENT(PLAYER_PED_ID(), FALSE, NULL)
                                    bIsPlayerControlDisabled = TRUE

                                    TASK_LOOK_AT_ENTITY(PLAYER_PED_ID(), objVendingMachine, 200, 2048, 2)
                                    SET_PED_RESET_FLAG(PLAYER_PED_ID(), 322, TRUE)
                                    TASK_GO_STRAIGHT_TO_COORD(PLAYER_PED_ID(), vVendInRangePoint, 1.0, 20000, GET_ENTITY_HEADING(objVendingMachine), 0.1)

                                    eVendingState = VENDING_STATE_GRAB_PLAYER
                                ENDIF
                            ENDIF
                        ELSE
                            DISPLAY_HELP("VENDCSH") // You don't have enough money to use the machine.
                        ENDIF
                    
                    ENDIF
                ENDIF
            ENDIF
            BREAK

        CASE VENDING_STATE_GRAB_PLAYER
            DISABLE_CONTROL_ACTIONS()

            IF GET_SCRIPT_TASK_STATUS(PLAYER_PED_ID(), 2106541073) == 7 AND GET_SCRIPT_TASK_STATUS(PLAYER_PED_ID(), 2106541073) <> 0 AND GET_SCRIPT_TASK_STATUS(PLAYER_PED_ID(), 2106541073) <> 1 AND IS_ENTITY_AT_COORD(PLAYER_PED_ID(), vVendInRangePoint, << 0.1, 0.1, 0.1 >>, FALSE, TRUE, 0)
                TASK_PLAY_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT1", 2.0, -4.0, -1, 1048576, 0.0, FALSE, 0, FALSE)
                eVendingState = VENDING_STATE_RUN_VEND
            ELIF HAS_PLAYER_BEEN_DAMAGED() // TODO: add timeout
                CLEAR_PED_TASKS(PLAYER_PED_ID())
                eVendingState = VENDING_STATE_PLAYER_OUT_OF_RANGE
            ENDIF
            BREAK

        CASE VENDING_STATE_RUN_VEND
            DISABLE_CONTROL_ACTIONS()

            IF IS_ENTITY_PLAYING_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT1", 3)
                IF GET_ENTITY_ANIM_CURRENT_TIME(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT1") < 0.52
                    IF NOT IS_ENTITY_AT_COORD(PLAYER_PED_ID(), vVendInRangePoint, << 0.1, 0.1, 0.1 >>, FALSE, TRUE, 0)
                        DISCARD_SODA_CAN(TRUE)
                        RESET_VENDING_STATE_MACHINE()
                    ENDIF
                ENDIF

                IF IS_ENTITY_PLAYING_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT1", 3)
                    IF GET_ENTITY_ANIM_CURRENT_TIME(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT1") > 0.31 // is it time to create the soda can?
                        IF DOES_ENTITY_EXIST(objSodaCan)
                            IF GET_ENTITY_ANIM_CURRENT_TIME(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT1") > 0.98 // is it time to change animation?
                                IF NOT IS_ENTITY_PLAYING_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT2", 3)
                                    TASK_PLAY_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT2", 4.0, -1000.0, -1, 1048576, 0.0, FALSE, 2052, FALSE)
                                    FORCE_PED_AI_AND_ANIMATION_UPDATE(PLAYER_PED_ID(), FALSE, FALSE)
                                ENDIF

                                SET_PLAYER_CONTROL(PLAYER_ID(), TRUE, 0)
                                SET_EVERYONE_IGNORE_PLAYER(PLAYER_ID(), TRUE)
                                bIsPlayerControlDisabled = FALSE

                                IF IS_ENTITY_PLAYING_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT1", 3)
                                    STOP_ANIM_TASK(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT1", -1.5)
                                ENDIF
                            ENDIF
                        ELSE
                            objSodaCan = CREATE_OBJECT_NO_OFFSET(GET_SODA_CAN_MODEL(), vVendInRangePoint, FALSE, FALSE, FALSE)
                            ATTACH_ENTITY_TO_ENTITY(objSodaCan, PLAYER_PED_ID(), GET_PED_BONE_INDEX(PLAYER_PED_ID(), 28422), 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, TRUE, TRUE, FALSE, FALSE, 2, TRUE)
                        ENDIF
                    ELIF GET_ENTITY_ANIM_CURRENT_TIME(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT1") > 0.1
                        // change money and stats should go here
                    ENDIF
                ENDIF
            ELIF IS_ENTITY_PLAYING_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT2", 3)
                IF GET_ENTITY_ANIM_CURRENT_TIME(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT2") > 0.98
                    IF NOT IS_ENTITY_PLAYING_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT3", 3)
                        TASK_PLAY_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT3", 1000.0, -4.0, -1, 1048624, 0.0, FALSE, 2048, FALSE)
                        FORCE_PED_AI_AND_ANIMATION_UPDATE(PLAYER_PED_ID(), FALSE, FALSE)
                    ENDIF
                    eVendingState = VENDING_STATE_RESET_VEND
                ENDIF
            ELSE
                RESET_VENDING_STATE_MACHINE()
            ENDIF
            BREAK

        CASE VENDING_STATE_RESET_VEND
            DISABLE_CONTROL_ACTIONS()

            IF IS_ENTITY_PLAYING_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT3", 3)
                IF GET_ENTITY_ANIM_CURRENT_TIME(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT3") > 0.306
                    ADD_SOLD_SODA_CAN()

                    bIsPlayerControlDisabled = FALSE
                    SET_EVERYONE_IGNORE_PLAYER(PLAYER_ID(), FALSE)
                    IF REQUEST_AMBIENT_AUDIO_BANK("VENDING_MACHINE", FALSE, -1)
                        RELEASE_AMBIENT_AUDIO_BANK()
                    ENDIF
                    HINT_AMBIENT_AUDIO_BANK("VENDING_MACHINE", FALSE, -1)
                    DISCARD_SODA_CAN(TRUE)
                    IF iNumberOfSodaCansSold >= k_iMaxNumberOfSodaCans
                        eState = SCRIPT_STATE_VENDING_MACHINE_EMPTY
                    ELSE
                        eVendingState = VENDING_STATE_PLAYER_OUT_OF_RANGE
                    ENDIF
                ENDIF
            ELSE
                SET_EVERYONE_IGNORE_PLAYER(PLAYER_ID(), FALSE)
                DISCARD_SODA_CAN(TRUE)
                RESET_VENDING_STATE_MACHINE()
            ENDIF
            BREAK
    ENDSWITCH
ENDPROC

PROC RESET_VENDING_STATE_MACHINE()
    SET_PLAYER_CONTROL(PLAYER_ID(), TRUE, 0)
    bIsPlayerControlDisabled = FALSE

    IF NOT IS_PED_INJURED(PLAYER_PED_ID())
        IF IS_ENTITY_PLAYING_ANIM(PLAYER_PED_ID(), sAnimDict, "PLYR_BUY_DRINK_PT1", 3) OR HAS_PLAYER_BEEN_DAMAGED()
            CLEAR_PED_TASKS(PLAYER_PED_ID())
        ENDIF
    ENDIF

    IF DOES_ENTITY_EXIST(objSodaCan)
        ADD_SOLD_SODA_CAN()
        DISCARD_SODA_CAN(FALSE)
    ENDIF

    eVendingState = VENDING_STATE_PLAYER_OUT_OF_RANGE
ENDPROC

PROC DISCARD_SODA_CAN(BOOL bDetachWithForce)
    DISABLE_CONTROL_ACTIONS()
    IF DOES_ENTITY_EXIST(objSodaCan)
        IF IS_ENTITY_ATTACHED(objSodaCan)
            DETACH_ENTITY(objSodaCan, true, true)
            IF bDetachWithForce
                APPLY_FORCE_TO_ENTITY(objSodaCan, 1, << 6.0, 10.0, 2.0 >>, << 0.0, 0.0, 0.0 >>, 0, TRUE, TRUE, FALSE, FALSE, TRUE)
            ENDIF
        ENDIF

        SET_OBJECT_AS_NO_LONGER_NEEDED(objSodaCan)
    ENDIF
ENDPROC

PROC ADD_SOLD_SODA_CAN()
    SET_ENTITY_HEALTH(PLAYER_PED_ID(), GET_PED_MAX_HEALTH(PLAYER_PED_ID()), 0)
    iNumberOfSodaCansSold += 1
ENDPROC

FUNC INT GET_SODA_CAN_MODEL()
    SWITCH eVendType
        CASE VENDING_MACHINE_SNACK
            RETURN HASH("prop_ld_snack_01")
    ENDSWITCH

    RETURN HASH("prop_ld_can_01b")
ENDFUNC

PROC DISABLE_CONTROL_ACTIONS()
	SET_PED_USING_ACTION_MODE(PLAYER_PED_ID(), FALSE, -1, NULL)
	DISABLE_CONTROL_ACTION(0, 21, TRUE)
	DISABLE_CONTROL_ACTION(0, 25, TRUE)
	DISABLE_CONTROL_ACTION(0, 24, TRUE)
	DISABLE_CONTROL_ACTION(0, 257, TRUE)
	DISABLE_CONTROL_ACTION(0, 141, TRUE)
	DISABLE_CONTROL_ACTION(0, 140, TRUE)
	DISABLE_CONTROL_ACTION(0, 142, TRUE)
	DISABLE_CONTROL_ACTION(0, 22, TRUE)
	DISABLE_CONTROL_ACTION(0, 44, TRUE)
	DISABLE_CONTROL_ACTION(0, 23, TRUE)
	DISABLE_CONTROL_ACTION(0, 47, TRUE)
	DISABLE_CONTROL_ACTION(0, 37, TRUE)
	DISABLE_CONTROL_ACTION(0, 28, TRUE)
	IF NOT IS_ENTITY_DEAD(PLAYER_PED_ID(), FALSE)
		SET_PED_MAX_MOVE_BLEND_RATIO(PLAYER_PED_ID(), 1.0)
		SET_PED_RESET_FLAG(PLAYER_PED_ID(), 102, TRUE)
		SET_PED_RESET_FLAG(PLAYER_PED_ID(), 322, TRUE)
	ENDIF
ENDPROC

FUNC BOOL ARE_ASSETS_READY()
    IF IS_STRING_NULL_OR_EMPTY(sAnimDict)
        sAnimDict = "MINI@SPRUNK@FIRST_PERSON"
    ENDIF

    REQUEST_ANIM_DICT(sAnimDict)
    IF HAS_ANIM_DICT_LOADED(sAnimDict)
        HINT_AMBIENT_AUDIO_BANK("VENDING_MACHINE", 0, -1)
        RETURN TRUE
    ENDIF

    RETURN FALSE
ENDFUNC

PROC TERMINATE()
    IF bIsPlayerControlDisabled
        SET_PLAYER_CONTROL(PLAYER_ID(), TRUE, 0)
    ENDIF

    IF NOT IS_STRING_NULL_OR_EMPTY(sAnimDict)
        REMOVE_ANIM_DICT(sAnimDict)
        SET_MODEL_AS_NO_LONGER_NEEDED(GET_SODA_CAN_MODEL())
        RELEASE_AMBIENT_AUDIO_BANK()
    ENDIF

    IF DOES_ENTITY_EXIST(objSodaCan) AND IS_ENTITY_A_MISSION_ENTITY(objSodaCan)
        DETACH_ENTITY(objSodaCan, TRUE, TRUE)
        DELETE_OBJECT(objSodaCan)
    ENDIF

    TERMINATE_THIS_THREAD()
ENDPROC

PROC DISPLAY_HELP(STRING sLabel)
    BEGIN_TEXT_COMMAND_DISPLAY_HELP(sLabel)
    END_TEXT_COMMAND_DISPLAY_HELP(0, FALSE, TRUE, -1)
ENDPROC

PROC DRAW_TEXT(STRING text, FLOAT x, FLOAT y)
    BEGIN_TEXT_COMMAND_DISPLAY_TEXT("STRING")
    ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(text)
    END_TEXT_COMMAND_DISPLAY_TEXT(x, y, 0)
ENDPROC

SCRIPT ob_vend(OBJECT_INDEX objVendingMachineArg)
    IF HAS_FORCE_CLEANUP_OCCURRED(2)
        TERMINATE()
    ENDIF

    objVendingMachine = objVendingMachineArg
    IF NOT DOES_ENTITY_EXIST(objVendingMachine)
        TERMINATE()
    ENDIF

    eVendType = GET_VENDING_MACHINE_TYPE(objVendingMachine)
    IF eVendType == VENDING_MACHINE_UNKNOWN
        TERMINATE()
    ENDIF

    eState = SCRIPT_STATE_WAITING_FOR_ASSETS
    WHILE TRUE
        WAIT(0)

        IF DOES_ENTITY_EXIST(objVendingMachine)
            IF IS_OBJECT_WITHIN_BRAIN_ACTIVATION_RANGE(objVendingMachine) AND NOT IS_ENTITY_DEAD(objVendingMachine, FALSE)
                TEXT_LABEL_127 tl = "VENDING MACHINE (type: "
                tl += ENUM_TO_INT(eVendType)
                tl += ", state: "
                tl += ENUM_TO_INT(eState)
                tl += ", vending state: "
                tl += ENUM_TO_INT(eVendingState)
                tl += ", sold: "
                tl += iNumberOfSodaCansSold
                tl += "/"
                tl += k_iMaxNumberOfSodaCans
                tl += ")"

                SET_TEXT_SCALE(0.3, 0.3)
                SET_TEXT_CENTRE(TRUE)
                DRAW_TEXT(tl, 0.5, 0.125)

                RUN_SCRIPT_STATE_MACHINE()
            ELSE
                TERMINATE()
            ENDIF
        ELSE
            TERMINATE()
        ENDIF
    ENDWHILE
ENDSCRIPT

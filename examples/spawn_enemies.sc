USING 'lib/entity_commands.sch'
USING 'lib/misc_commands.sch'
USING 'lib/pad_commands.sch'
USING 'lib/ped_commands.sch'
USING 'lib/script_commands.sch'
USING 'lib/streaming_commands.sch'
USING 'lib/system_commands.sch'
USING 'lib/weapon_commands.sch'

CONST INT STATE_WAITING_INPUT = 0
CONST INT STATE_SPAWNING_ENEMIES = 1

CONST INT MAX_ENEMIES = 32

INT nState = STATE_WAITING_INPUT
PED_INDEX aEnemies[MAX_ENEMIES]
INT nEnemyCount = 0

SCRIPT spawn_enemies
    WHILE TRUE
        WAIT(0)

        SWITCH nState
            CASE STATE_WAITING_INPUT
                CHECK_INPUT()
                BREAK
            CASE STATE_SPAWNING_ENEMIES
                TRY_SPAWN_ENEMIES()
                BREAK
        ENDSWITCH
    ENDWHILE
ENDSCRIPT

PROC CHECK_INPUT()
    IF IS_CONTROL_PRESSED(0, INPUT_CONTEXT_SECONDARY) AND IS_CONTROL_JUST_PRESSED(0, INPUT_CONTEXT)
        nState = STATE_SPAWNING_ENEMIES
    ENDIF
    
    IF IS_CONTROL_PRESSED(0, INPUT_CONTEXT_SECONDARY) AND IS_CONTROL_JUST_PRESSED(0, INPUT_RELOAD)
        DELETE_ENEMIES()
    ENDIF
ENDPROC

PROC TRY_SPAWN_ENEMIES()
    DO_SPAWN_ENEMIES()
    IF nEnemyCount > 0
        nState = STATE_WAITING_INPUT
    ENDIF
ENDPROC


PROC DO_SPAWN_ENEMIES()
    DELETE_ENEMIES()

    INT enemyModel = GET_ENEMY_MODEL()
    IF IS_MODEL_AVAILABLE(enemyModel)
        nEnemyCount = GET_RANDOM_INT_IN_RANGE(1, MAX_ENEMIES + 1)
        VECTOR spawnPos = <<0.0, 0.0, 70.0>>
        INT i
        REPEAT nEnemyCount i
            PED_INDEX ped = CREATE_PED(PED_TYPE_CRIMINAL, enemyModel, spawnPos, 0.0, FALSE, FALSE)
            GIVE_WEAPON_TO_PED(ped, GET_RANDOM_WEAPON(), 9999999, FALSE, TRUE)
            SET_PED_COMBAT_MOVEMENT(ped, PED_CM_WILL_ADVANCE)
            SET_PED_TARGET_LOSS_RESPONSE(ped, PED_TLR_SEARCH_FOR_TARGET)

            aEnemies[i] = ped
            spawnPos.x = spawnPos.x + 0.5
        ENDREPEAT
    ENDIF
ENDPROC

PROC DELETE_ENEMIES()
    INT i
    REPEAT nEnemyCount i
        IF DOES_ENTITY_EXIST(aEnemies[i])
            DELETE_PED(aEnemies[i])
        ENDIF
    ENDREPEAT
    nEnemyCount = 0
ENDPROC

FUNC BOOL IS_MODEL_AVAILABLE(INT modelHash)
    IF modelHash == 0
        RETURN FALSE
    ENDIF

    REQUEST_MODEL(modelHash)
    RETURN HAS_MODEL_LOADED(modelHash)
ENDFUNC

FUNC INT GET_ENEMY_MODEL()
    RETURN GET_HASH_KEY("s_m_m_chemsec_01")
ENDFUNC

FUNC INT GET_RANDOM_WEAPON()
    SWITCH GET_RANDOM_INT_IN_RANGE(0, 4)
        CASE 0
            RETURN GET_HASH_KEY("WEAPON_PISTOL")
        CASE 1
            RETURN GET_HASH_KEY("WEAPON_MICROSMG")
        CASE 2
            RETURN GET_HASH_KEY("WEAPON_ASSAULTSHOTGUN")
        CASE 3
            RETURN GET_HASH_KEY("WEAPON_CARBINERIFLE")
        CASE 4
            RETURN GET_HASH_KEY("WEAPON_REVOLVER")
    ENDSWITCH
ENDFUNC
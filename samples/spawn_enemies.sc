SCRIPT_NAME spawn_enemies

USING 'lib/entity_commands.sch'
USING 'lib/misc_commands.sch'
USING 'lib/pad_commands.sch'
USING 'lib/ped_commands.sch'
USING 'lib/script_commands.sch'
USING 'lib/streaming_commands.sch'
USING 'lib/system_commands.sch'

CONST INT STATE_WAITING_INPUT = 0
CONST INT STATE_SPAWNING_ENEMIES = 1

CONST INT MAX_ENEMIES = 32

INT nState = STATE_WAITING_INPUT
PED_INDEX aEnemies[MAX_ENEMIES]
INT nEnemyCount = 0

PROC MAIN()
    WHILE TRUE
        WAIT(0)

        // TODO: replace with SWITCH when supported
        IF nState == STATE_WAITING_INPUT
            CHECK_INPUT()
        ELSE
            IF nState == STATE_SPAWNING_ENEMIES
                TRY_SPAWN_ENEMIES()
            ENDIF
        ENDIF
    ENDWHILE
ENDPROC

PROC CHECK_INPUT()
    IF IS_CONTROL_PRESSED(0, INPUT_CONTEXT_SECONDARY) AND IS_CONTROL_JUST_PRESSED(0, INPUT_CONTEXT)
        nState = STATE_SPAWNING_ENEMIES
    ENDIF
    
    IF IS_CONTROL_PRESSED(0, INPUT_CONTEXT_SECONDARY) AND IS_CONTROL_JUST_PRESSED(0, INPUT_RELOAD)
        DELETE_ENEMIES()
    ENDIF
ENDPROC

PROC TRY_SPAWN_ENEMIES()
    SPAWN_ENEMIES()
    IF nEnemyCount > 0
        nState = STATE_WAITING_INPUT
    ENDIF
ENDPROC


PROC SPAWN_ENEMIES()
    DELETE_ENEMIES()

    INT enemyModel = GET_ENEMY_MODEL()
    IF IS_MODEL_AVAILABLE(enemyModel)
        nEnemyCount = GET_RANDOM_INT_IN_RANGE(1, MAX_ENEMIES + 1)
        VEC3 spawnPos = <<0.0, 0.0, 70.0>>
        INT i = 0
        WHILE i < nEnemyCount
            PED_INDEX ped = CREATE_PED(PED_TYPE_CRIMINAL, enemyModel, spawnPos, 0.0, FALSE, FALSE)

            aEnemies[i] = ped
            spawnPos.x = spawnPos.x + 0.5
            i = i + 1
        ENDWHILE
    ENDIF
ENDPROC

PROC DELETE_ENEMIES()
    INT i = 0
    WHILE i < nEnemyCount
        IF DOES_ENTITY_EXIST(aEnemies[i].base)
            DELETE_PED(aEnemies[i])
        ENDIF
        i = i + 1
    ENDWHILE
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
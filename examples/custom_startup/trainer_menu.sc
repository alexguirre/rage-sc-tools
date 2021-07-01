
USING 'menu.sch'
USING 'natives.sch'

MENU sTrainerMenu
BOOL bTrainerMenuVisible = FALSE
BOOL bSpawnCar = FALSE, bGiveWeapons = FALSE
VEHICLE_INDEX vehSpawned = NULL

SCRIPT trainer_menu
    MENU_INIT(sTrainerMenu, "Trainer")
    MENU_ADD_ITEM(sTrainerMenu, "Spawn Car", SPAWN_CAR_CALLBACK)
    MENU_ADD_ITEM(sTrainerMenu, "Give Weapons", GIVE_WEAPONS_CALLBACK)
    INT i
    REPEAT 6 i
        TEXT_LABEL_15 tlName = "Item #"
        APPEND(tlName, i + 3)
        MENU_ADD_ITEM(sTrainerMenu, tlName, EMPTY_CALLBACK)
    ENDREPEAT

    WHILE TRUE
        IF IS_CONTROL_JUST_PRESSED(2, INPUT_REPLAY_START_STOP_RECORDING)
            bTrainerMenuVisible = NOT bTrainerMenuVisible
        ENDIF

        IF bTrainerMenuVisible
            MENU_PROCESS_CONTROLS(sTrainerMenu)
            MENU_DRAW(sTrainerMenu)
        ENDIF

        IF bSpawnCar
            IF DOES_ENTITY_EXIST(vehSpawned)
                SET_VEHICLE_AS_NO_LONGER_NEEDED(vehSpawned)
                vehSpawned = NULL
            ENDIF

            REQUEST_MODEL(`zentorno`)
            IF HAS_MODEL_LOADED(`zentorno`)
                VECTOR vPlayerPos = GET_ENTITY_COORDS(PLAYER_PED_ID(), FALSE)
                vehSpawned = CREATE_VEHICLE(`zentorno`, vPlayerPos, 0.0, FALSE, FALSE, FALSE)
                SET_MODEL_AS_NO_LONGER_NEEDED(`zentorno`)
                bSpawnCar = FALSE
            ENDIF
        ENDIF

        IF bGiveWeapons
            GIVE_WEAPON_TO_PED(PLAYER_PED_ID(), `WEAPON_PISTOL`, 999, FALSE, FALSE)
            GIVE_WEAPON_TO_PED(PLAYER_PED_ID(), `WEAPON_ASSAULTRIFLE`, 999, FALSE, FALSE)
            GIVE_WEAPON_TO_PED(PLAYER_PED_ID(), `WEAPON_PUMPSHOTGUN`, 999, FALSE, FALSE)
            GIVE_WEAPON_TO_PED(PLAYER_PED_ID(), `WEAPON_RPG`, 999, FALSE, FALSE)
            bGiveWeapons = FALSE
        ENDIF

        WAIT(0)
    ENDWHILE
ENDSCRIPT

PROC SPAWN_CAR_CALLBACK(MENU_ITEM& sItem)
    bSpawnCar = TRUE
ENDPROC

PROC GIVE_WEAPONS_CALLBACK(MENU_ITEM& sItem)
    bGiveWeapons = TRUE
ENDPROC

PROC EMPTY_CALLBACK(MENU_ITEM& sItem)
ENDPROC
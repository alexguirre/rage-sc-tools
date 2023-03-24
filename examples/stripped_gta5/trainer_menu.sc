
USING 'menu.sch'
USING 'natives.sch'

MENU sTrainerMenu
BOOL bTrainerMenuVisible = FALSE
BOOL bSpawnCar = FALSE, bGiveWeapons = FALSE, bToggleFreecam = FALSE
VEHICLE_INDEX vehSpawned = NULL
CAMERA_INDEX camFreecam = NULL
BOOL bFreecam = FALSE

TEXT_LABEL_31 tlScriptToStart
BOOL bStartScript = FALSE

PROC SPAWN_CAR_CALLBACK()
    bSpawnCar = TRUE
ENDPROC

PROC GIVE_WEAPONS_CALLBACK()
    bGiveWeapons = TRUE
ENDPROC

PROC TOGGLE_FREECAM_CALLBACK()
    bToggleFreecam = TRUE
ENDPROC

PROC START_SCRIPT_1_CALLBACK()
    bStartScript = TRUE
    tlScriptToStart = "fps_display"
ENDPROC

PROC START_SCRIPT_2_CALLBACK()
    bStartScript = TRUE
    tlScriptToStart = "random_vehicle_colors"
ENDPROC

PROC START_SCRIPT_3_CALLBACK()
    bStartScript = TRUE
    tlScriptToStart = "spawn_enemies"
ENDPROC

SCRIPT trainer_menu
    MENU_INIT(sTrainerMenu, "Trainer")
    MENU_ADD_ITEM(sTrainerMenu, "Spawn Car", SPAWN_CAR_CALLBACK)
    MENU_ADD_ITEM(sTrainerMenu, "Give Weapons", GIVE_WEAPONS_CALLBACK)
    MENU_ADD_ITEM(sTrainerMenu, "Toggle Freecam", TOGGLE_FREECAM_CALLBACK)
    MENU_ADD_ITEM(sTrainerMenu, "Start Script 'fps_display'", START_SCRIPT_1_CALLBACK)
    MENU_ADD_ITEM(sTrainerMenu, "Start Script 'random_vehicle_colors'", START_SCRIPT_2_CALLBACK)
    MENU_ADD_ITEM(sTrainerMenu, "Start Script 'spawn_enemies'", START_SCRIPT_3_CALLBACK)
    // INT i
    // REPEAT 5 i
    //     TEXT_LABEL_15 tlName = "Item #"
    //     tlName += i + 4
    //     MENU_ADD_ITEM(sTrainerMenu, tlName, NULL)
    // ENDREPEAT

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

            REQUEST_MODEL(HASH("zentorno"))
            IF HAS_MODEL_LOADED(HASH("zentorno"))
                VECTOR vPlayerPos = GET_ENTITY_COORDS(PLAYER_PED_ID(), FALSE)
                vehSpawned = CREATE_VEHICLE(HASH("zentorno"), vPlayerPos, 0.0, FALSE, FALSE, FALSE)
                SET_MODEL_AS_NO_LONGER_NEEDED(HASH("zentorno"))
                bSpawnCar = FALSE
            ENDIF
        ENDIF

        IF bGiveWeapons
            GIVE_WEAPON_TO_PED(PLAYER_PED_ID(), HASH("WEAPON_PISTOL"), 999, FALSE, FALSE)
            GIVE_WEAPON_TO_PED(PLAYER_PED_ID(), HASH("WEAPON_ASSAULTRIFLE"), 999, FALSE, FALSE)
            GIVE_WEAPON_TO_PED(PLAYER_PED_ID(), HASH("WEAPON_PUMPSHOTGUN"), 999, FALSE, FALSE)
            GIVE_WEAPON_TO_PED(PLAYER_PED_ID(), HASH("WEAPON_RPG"), 999, FALSE, FALSE)
            bGiveWeapons = FALSE
        ENDIF

        IF bToggleFreecam
            bFreecam = NOT bFreecam
            ENABLE_FREECAM(bFreecam)
            bToggleFreecam = FALSE
        ENDIF

        IF DOES_CAM_EXIST(camFreecam)
            PROCESS_FREECAM_CONTROLS()
        ENDIF

        IF bStartScript
            REQUEST_SCRIPT(tlScriptToStart)
            IF HAS_SCRIPT_LOADED(tlScriptToStart)
                START_NEW_SCRIPT(tlScriptToStart, 512)
                SET_SCRIPT_AS_NO_LONGER_NEEDED(tlScriptToStart)
                tlScriptToStart = ""
                bStartScript = FALSE
            ENDIF
        ENDIF

        WAIT(0)
    ENDWHILE
ENDSCRIPT

PROC ENABLE_FREECAM(BOOL bEnable)
    IF bEnable
        IF NOT DOES_CAM_EXIST(camFreecam)
            camFreecam = CREATE_CAM_WITH_PARAMS("DEFAULT_SCRIPTED_CAMERA", GET_ENTITY_COORDS(PLAYER_PED_ID(), FALSE), <<0.0, 0.0, 0.0>>, 90.0, TRUE, 2)
            RENDER_SCRIPT_CAMS(TRUE, FALSE, 3000, TRUE, FALSE, 0)

            SET_PLAYER_CONTROL(PLAYER_ID(), FALSE, 0)
        ENDIF
    ELSE
        IF DOES_CAM_EXIST(camFreecam)
            IF IS_CAM_ACTIVE(camFreecam)
                RENDER_SCRIPT_CAMS(FALSE, FALSE, 3000, TRUE, FALSE, 0)
                SET_CAM_ACTIVE(camFreecam, FALSE)
            ENDIF
            DESTROY_CAM(camFreecam, FALSE)
            camFreecam = NULL

            SET_PLAYER_CONTROL(PLAYER_ID(), TRUE, 0)
        ENDIF
    ENDIF
ENDPROC

CONST_FLOAT FREECAM_MOVE_SPEED 15.0   // meters/second
CONST_FLOAT FREECAM_ROT_SPEED 10.0    // degrees/second

PROC PROCESS_FREECAM_CONTROLS()
    FLOAT fLookLR = GET_CONTROL_NORMAL(2, INPUT_SCALED_LOOK_LR)
    FLOAT fLookUD = GET_CONTROL_NORMAL(2, INPUT_SCALED_LOOK_UD)

    IF fLookLR <> 0.0 OR fLookUD <> 0.0
        VECTOR vRot = GET_CAM_ROT(camFreecam, 2)
        vRot.x -= FREECAM_ROT_SPEED * fLookUD
        vRot.z -= FREECAM_ROT_SPEED * fLookLR

        SET_CAM_ROT(camFreecam, vRot, 2)
    ENDIF


    FLOAT fLeftRight = GET_CONTROL_NORMAL(2, INPUT_MOVE_LR)
    FLOAT fForwardBack = GET_CONTROL_NORMAL(2, INPUT_MOVE_UD)

    IF fLeftRight <> 0.0 OR fForwardBack <> 0.0
        VECTOR vRot = GET_CAM_ROT(camFreecam, 2)
        VECTOR vDir = ROT_TO_DIR(vRot)
        VECTOR vRight = NORMALISE_VECTOR(VCROSS(vDir, <<0.0, 0.0, 1.0>>))
        FLOAT fOffsetRight = FREECAM_MOVE_SPEED * fLeftRight * GET_FRAME_TIME()
        FLOAT fOffsetForward = -FREECAM_MOVE_SPEED * fForwardBack * GET_FRAME_TIME()
        VECTOR vOffset = vDir * F2V(fOffsetForward) + vRight * F2V(fOffsetRight)

        SET_CAM_COORD(camFreecam, GET_CAM_COORD(camFreecam) + vOffset)
    ENDIF
ENDPROC

FUNC VECTOR ROT_TO_DIR(VECTOR vRot)
    FLOAT fCos = ABSF(COS(vRot.x))
    RETURN << -SIN(vRot.z) * fCos, COS(vRot.z) * fCos, SIN(vRot.x) >>
ENDFUNC

FUNC VECTOR VCROSS(VECTOR a, VECTOR b)
    FLOAT x = a.y * b.z - a.z * b.y
    FLOAT y = a.z * b.x - a.x * b.z
    FLOAT z = a.x * b.y - a.y * b.x
    RETURN <<x, y, z>>
ENDFUNC

FUNC VECTOR NORMALISE_VECTOR(VECTOR v)
    RETURN v * F2V(1.0 / VMAG(v))
ENDFUNC

USING 'natives.sch'

STRUCT MENU
    TEXT_LABEL_15 tlTitle
    MENU_ITEM sItems[32]
    INT iItemCount = 0
    INT iSelectedItem = 0
    INT iLastInputTime = 0
ENDSTRUCT

STRUCT MENU_ITEM
    TEXT_LABEL_15 tlName
    MENU_ITEM_CALLBACK procCallback
ENDSTRUCT

PROTO PROC MENU_ITEM_CALLBACK(MENU_ITEM& item)

CONST INT MENU_TIME_BETWEEN_INPUTS = 175

PROC MENU_INIT(MENU& sMenu, STRING tlTitle)
    MENU sResultMenu
    sResultMenu.tlTitle = tlTitle

    sMenu = sResultMenu
ENDPROC

PROC MENU_PROCESS_CONTROLS(MENU& sMenu)

    IF IS_CONTROL_PRESSED(2, INPUT_FRONTEND_UP) AND (GET_GAME_TIMER() - sMenu.iLastInputTime) > MENU_TIME_BETWEEN_INPUTS

        sMenu.iLastInputTime = GET_GAME_TIMER()
        PLAY_SOUND_FRONTEND(-1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", FALSE)
        sMenu.iSelectedItem -= 1

        IF sMenu.iSelectedItem < 0
            sMenu.iSelectedItem = sMenu.iItemCount - 1
        ENDIF

    ELIF IS_CONTROL_PRESSED(2, INPUT_FRONTEND_DOWN)  AND (GET_GAME_TIMER() - sMenu.iLastInputTime) > MENU_TIME_BETWEEN_INPUTS

        sMenu.iLastInputTime = GET_GAME_TIMER()
        PLAY_SOUND_FRONTEND(-1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", FALSE)
        sMenu.iSelectedItem += 1

        IF sMenu.iSelectedItem >= sMenu.iItemCount
            sMenu.iSelectedItem = 0
        ENDIF

    ENDIF

    IF sMenu.iSelectedItem >= 0 AND sMenu.iSelectedItem < sMenu.iItemCount AND IS_CONTROL_JUST_PRESSED(2, INPUT_FRONTEND_ACCEPT)
        sMenu.sItems[sMenu.iSelectedItem].procCallback(sMenu.sItems[sMenu.iSelectedItem])
    ENDIF

ENDPROC

PROC MENU_DRAW(MENU& sMenu)
    SET_TEXT_SCALE(0.6, 0.6)
    SET_TEXT_CENTRE(TRUE)
    BEGIN_TEXT_COMMAND_DISPLAY_TEXT("STRING")
    ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(sMenu.tlTitle)
    END_TEXT_COMMAND_DISPLAY_TEXT(0.1, 0.05, 0)

    INT i
    REPEAT sMenu.iItemCount i
        MENU_DRAW_ITEM(sMenu.sItems[i], sMenu.iSelectedItem == i, 0.1, 0.125 + 0.04 * I2F(i))
    ENDREPEAT
ENDPROC

PROC MENU_DRAW_ITEM(MENU_ITEM& sItem, BOOL bSelected, FLOAT x, FLOAT y)

    // draw background
    IF bSelected
        DRAW_RECT(x, y, 0.09, 0.04, 240, 240, 240, 200, FALSE)
        SET_TEXT_COLOUR(0, 0, 0, 255)
    ELSE
        DRAW_RECT(x, y, 0.09, 0.04, 10, 10, 10, 200, FALSE)
        SET_TEXT_COLOUR(240, 240, 240, 255)
    ENDIF

    // draw name
    SET_TEXT_SCALE(0.3, 0.3)
    SET_TEXT_CENTRE(TRUE)
    BEGIN_TEXT_COMMAND_DISPLAY_TEXT("STRING")
    ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(sItem.tlName)
    END_TEXT_COMMAND_DISPLAY_TEXT(x, y - 0.01, 0)
ENDPROC

PROC MENU_ADD_ITEM(MENU& sMenu, STRING tlName, MENU_ITEM_CALLBACK procCallback)
    IF sMenu.iItemCount >= COUNT_OF(sMenu.sItems)
        // menu is full
        RETURN
    ENDIF
    
    MENU_ITEM sItem
    sItem.tlName = tlName
    sItem.procCallback = procCallback

    sMenu.sItems[sMenu.iItemCount] = sItem
    sMenu.iItemCount += 1
ENDPROC
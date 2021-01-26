USING 'lib/misc_commands.sch'
USING 'lib/hud_commands.sch'
USING 'lib/system_commands.sch'

PROC MAIN()
    WHILE TRUE
        WAIT(0)

        FLOAT frameTime = GET_FRAME_TIME()
        INT fps = FLOOR(1.0 / frameTime)

        TEXT_LABEL64 text
        ASSIGN_STRING(text, "FPS: ")
        APPEND_INT(text, fps)

        BEGIN_TEXT_COMMAND_DISPLAY_TEXT("STRING")
        ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(text)
        END_TEXT_COMMAND_DISPLAY_TEXT(0.01, 0.01, 0)
    ENDWHILE
ENDPROC
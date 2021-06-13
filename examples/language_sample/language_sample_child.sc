SCRIPT_NAME language_sample_child
SCRIPT_HASH `language_sample_child`

USING 'language_sample_shared.sch'

ARG CHILD_ARGS theArgs

PROC MAIN()
    g_nNumberOfChildScriptsRunning += 1

    WAIT(30000)

    g_nNumberOfChildScriptsRunning -= 1
    TERMINATE_THIS_THREAD()
ENDPROC
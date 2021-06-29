SCRIPT_HASH `language_sample_child`

USING 'language_sample_shared.sch'

SCRIPT language_sample_child(CHILD_ARGS theArgs)
    g_nNumberOfChildScriptsRunning += 1

    WAIT(30000)

    g_nNumberOfChildScriptsRunning -= 1
    TERMINATE_THIS_THREAD()
ENDSCRIPT
namespace ScTools.LanguageServer;

using System;
using System.CommandLine;
using System.Diagnostics;

internal static class Program
{
    private static int Main(string[] args)
    {
        var rootCmd = new RootCommand("Language server for ScriptLang (.sc).")
        {
            new Option<bool>("--launch-debugger", () => false)
        };
        rootCmd.SetHandler<bool>(Run, rootCmd.Options[0]);
        return rootCmd.Invoke(args);
    }

    private static void Run(bool launchDebugger)
    {
        if (launchDebugger)
        {
            Debugger.Launch();
        }

        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();

        using var server = new Server(stdout, stdin);

        server.WaitForExit();
    }
}

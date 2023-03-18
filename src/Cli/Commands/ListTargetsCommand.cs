namespace ScTools.Cli.Commands;

using System;
using System.CommandLine;
using ScTools.ScriptLang.Workspace;

internal static class ListTargetsCommand
{
    public static Command Command { get; } = BuildCommand();

    private static Command BuildCommand()
    {
        var cmd = new Command("list-targets", "Display the supported build targets");
        cmd.SetHandler(Invoke);
        return cmd;
    }

    public static void Invoke()
    {
        var targets = new BuildTarget[]
        {
            new(Game.GTAV, Platform.x64),
            new(Game.GTAIV, Platform.x86),
            new(Game.MC4, Platform.Xenon),
        };
        foreach (var target in targets)
        {
            Console.WriteLine(target);
        }
    }
}

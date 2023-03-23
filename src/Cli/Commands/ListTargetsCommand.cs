namespace ScTools.Cli.Commands;

using System.CommandLine;
using Spectre.Console;
using ScTools.GameFiles;

internal static class ListTargetsCommand
{
    public static Command Command { get; } = BuildCommand();

    private static Command BuildCommand()
    {
        var cmd = new Command("list-targets", "Display the supported build targets.");
        cmd.SetHandler(Invoke);
        return cmd;
    }

    public static void Invoke()
    {
        Keys.LoadAll();

        var targets = new (string Game, string Platform, string Id, bool EncryptionKey, bool Compile, bool Decompile)[]
        {
            ("Grand Theft Auto V", "x64", "gta5-x64", CodeWalker.GameFiles.GTA5Keys.PC_AES_KEY != null, true, false),
            ("Grand Theft Auto IV", "x86", "gta4-x86", Keys.NY.AesKeyPC.Length != 0, true, false),
            ("Max Payne 3", "x86", "mp3-x86", Keys.Payne.AesKeyPC.Length != 0, false, false),
            ("Midnight Club: Los Angeles", "Xenon", "mc4-xenon", Keys.MC4.AesKeyXenon.Length != 0, false, false),
            ("Red Dead Redemption", "Xenon", "rdr2-xenon", Keys.RDR2.AesKeyXenon.Length != 0, false, false),
        };

        const string yes = "[green]Yes[/]", no = "[red]No[/]";
        static string YesNo(bool value) => value ? yes : no;

        var table = new Table();
        table.AddColumns("Game", "Platform", "Identifier", "Found encription key?", "Can compile?", "Can decompile?");
        foreach (var t in targets)
        {
            table.AddRow(t.Game, t.Platform, t.Id, YesNo(t.EncryptionKey), YesNo(t.Compile), YesNo(t.Decompile));
        }
        Std.Out.Write(table);
    }
}

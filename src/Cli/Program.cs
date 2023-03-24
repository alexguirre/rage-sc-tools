namespace ScTools.Cli;

using System;
using System.IO;
using System.CommandLine;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ScTools.Cli.Commands;
using ScTools.GameFiles;

internal static class Program
{
    private static readonly Lazy<string> dataDirectory = new(() =>
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScTools");
        Directory.CreateDirectory(path);
        return path;
    });

    private static readonly Lazy<KeyStore> keys = new(() =>
    {
        // TODO: make Keys paths user configurable
        return GameFiles.KeyStore.LoadAll(
            cacheDirectory: DataDirectory,
            gta5ExePath: "D:\\programs\\Rockstar Games\\Grand Theft Auto V\\GTA5.exe",
            gta4ExePath: "D:\\programs\\SteamLibrary\\steamapps\\common\\Grand Theft Auto IV\\GTAIV\\GTAIV.exe",
            mc4XexPath: "D:\\media\\mcla\\default.unencrypted.xex",
            rdr2XexPath: "D:\\media\\rdr2\\default.unencrypted.xex",
            mp3ExePath: "D:\\programs\\SteamLibrary\\steamapps\\common\\Max Payne 3\\Max Payne 3\\MaxPayne3.exe");
    });

    public static string DataDirectory => dataDirectory.Value;
    public static KeyStore Keys => keys.Value;

    private static async Task<int> Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        var rootCmd = new RootCommand("Tool for working with RAGE scripts.");
        rootCmd.AddCommand(BuildProjectCommand.Command);
        rootCmd.AddCommand(CompileCommand.Command);
        rootCmd.AddCommand(DumpCommand.Command);
        rootCmd.AddCommand(InitProjectCommand.Command);
        rootCmd.AddCommand(ListTargetsCommand.Command);
        return await rootCmd.InvokeAsync(args);
    }
}

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
        var c = Configuration;
        return GameFiles.KeyStore.LoadAll(
            cacheDirectory: DataDirectory,
            gta5ExePath: c.GTA5ExePath,
            gta4ExePath: c.GTA4ExePath,
            mc4XexPath: c.MC4XexPath,
            rdr2XexPath: c.RDR2XexPath,
            mp3ExePath: c.MP3ExePath);
    });

    private static readonly Lazy<string> configurationPath = new(() => Path.Combine(DataDirectory, "config.json"));

    private static Lazy<Configuration> configuration = new(() =>
    {
        var path = ConfigurationPath;
        if (!File.Exists(path))
        {
            return Configuration.Default;
        }

        using var file = File.OpenRead(path);
        return Configuration.FromJson(file) ?? throw new InvalidDataException($"Could not read configuration file '{path}'");
    });

    public static string DataDirectory => dataDirectory.Value;
    public static KeyStore Keys => keys.Value;
    public static string ConfigurationPath => configurationPath.Value;

    public static Configuration Configuration
    {
        get => configuration.Value;
        set
        {
            if (value == configuration.Value)
            {
                return;
            }

            var path = ConfigurationPath;
            using var file = File.Open(path, FileMode.Create);
            value.ToJson(file);
            configuration = new(() => value);
        }
    }

    private static async Task<int> Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        var rootCmd = new RootCommand("Tool for working with RAGE scripts.");
        rootCmd.AddCommand(BuildProjectCommand.Command);
        rootCmd.AddCommand(CompileCommand.Command);
        rootCmd.AddCommand(ConfigCommand.Command);
        rootCmd.AddCommand(DumpCommand.Command);
        rootCmd.AddCommand(InitProjectCommand.Command);
        rootCmd.AddCommand(ListTargetsCommand.Command);
        return await rootCmd.InvokeAsync(args);
    }
}

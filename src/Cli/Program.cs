namespace ScTools.Cli;

using System;
using System.IO;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ScTools.Cli.Commands;
using ScTools.GameFiles;
using Spectre.Console;

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

    private static RootCommand RootCommand { get; } = new("Tool for working with RAGE scripts.")
    {
        BuildProjectCommand.Command,
        CompileCommand.Command,
        ConfigCommand.Command,
        DumpCommand.Command,
        InitProjectCommand.Command,
        ListTargetsCommand.Command,
    };

    private static async Task<int> Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        var parser = new CommandLineBuilder(RootCommand)
            .UseDefaults()
            .UseHelp(CustomizeHelp)
            .Build();

        return await parser.InvokeAsync(args);
    }

    private static void CustomizeHelp(HelpContext ctx)
    {
        ctx.HelpBuilder.CustomizeLayout(
            _ =>
                HelpBuilder.Default
                    .GetLayout()
                    .Append(_ => PrintGettingStartedSection()));
    }

    private static void PrintGettingStartedSection()
    {
        var gettingStarted = $"""
            First, start by configuring the paths to the game executables. This is required to extract the encryption keys required to encrypt or decrypt the scripts. You only need to do this for the games you want to compile scripts for.

              [blue]>[/] {RootCommand.Name} {ConfigCommand.Command.Name} set GTA5ExePath "C:\path\to\GTA5.exe"
              [blue]>[/] {RootCommand.Name} {ConfigCommand.Command.Name} set GTA4ExePath "C:\path\to\GTAIV.exe"
              [blue]>[/] {RootCommand.Name} {ConfigCommand.Command.Name} set MP3ExePath "C:\path\to\MP3.exe"
              [blue]>[/] {RootCommand.Name} {ConfigCommand.Command.Name} set MC4XexPath "C:\path\to\default.xex"
              [blue]>[/] {RootCommand.Name} {ConfigCommand.Command.Name} set RDR2XexPath "C:\path\to\default.xex"

            Then you can create a new project with:

              [blue]>[/] {RootCommand.Name} {InitProjectCommand.Command.Name} my_project gta5-x64 my_project_dir

            And build the project:

              [blue]>[/] {RootCommand.Name} {BuildProjectCommand.Command.Name} my_project_dir/my_project.scproj

            Or from the project directory:

              [blue]>[/] cd my_project_dir
              [blue]>[/] {RootCommand.Name} {BuildProjectCommand.Command.Name}
            """;
        Std.Out.WriteLine();
        Std.Out.WriteLine("Getting started:");
        Std.Out.Write(new Padder(
            new Markup(gettingStarted),
            new Padding(2, 0)));
    }
}

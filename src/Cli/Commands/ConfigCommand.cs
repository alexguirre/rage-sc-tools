namespace ScTools.Cli.Commands;

using System;
using System.CommandLine;
using System.Linq;
using Spectre.Console;
using Spectre.Console.Rendering;

internal static class ConfigCommand
{
    public static Command Command { get; } = BuildCommand();

    private static Command BuildCommand()
    {
        var cmd = new Command("config", "Show or edit the configuration file.");
        cmd.AddCommand(GetCommand.SubCommand);
        cmd.AddCommand(SetCommand.SubCommand);
        cmd.SetHandler(Invoke);
        return cmd;
    }

    public static void Invoke()
    {
        var c = Program.Configuration;

        var grid = new Grid();
        grid.AddColumn().AddColumn();

        IRenderable displayPath(string? path) => path != null ? new TextPath(path) : new Text("null", new(foreground: Color.Red));
        grid.AddRow(new Text("Configuration File"), new TextPath(Program.ConfigurationPath));
        grid.AddEmptyRow();
        grid.AddRow(new Text(nameof(Configuration.GTA5ExePath)), displayPath(c.GTA5ExePath));
        grid.AddRow(new Text(nameof(Configuration.GTA4ExePath)), displayPath(c.GTA4ExePath));
        grid.AddRow(new Text(nameof(Configuration.MC4XexPath)), displayPath(c.MC4XexPath));
        grid.AddRow(new Text(nameof(Configuration.RDR2XexPath)), displayPath(c.RDR2XexPath));
        grid.AddRow(new Text(nameof(Configuration.MP3ExePath)), displayPath(c.MP3ExePath));
        
        Std.Out.Write(grid);
    }

    private static void ValidateOption(System.CommandLine.Parsing.ArgumentResult result)
    {
        var s = result.Tokens.SingleOrDefault()?.Value;
        if (s is not null)
        {
            var cmp = System.StringComparer.OrdinalIgnoreCase;
            if (Configuration.OptionNames.All(option => !cmp.Equals(s, option)))
            {
                result.ErrorMessage = LocalizationResources.Instance.UnrecognizedArgument(s, Configuration.OptionNames);
            }
        }
    }

    private static class SetCommand
    {
        public static readonly Argument<string> Option = new Argument<string>(
                "option", "The option to modify.")
            .AddCompletions(Configuration.OptionNames)
            .AddValidatorEx(ValidateOption);
        public static readonly Argument<string> Value = new(
            "value", "The new value for the option. Use 'null' to unset the option.");

        public static Command SubCommand { get; } = BuildSubCommand();

        private static Command BuildSubCommand()
        {
            var cmd = new Command("set", "Set an option to a new value.")
            {
                Option, Value
            };
            cmd.SetHandler(Invoke, Option, Value);
            return cmd;
        }

        public static void Invoke(string option, string value)
        {
            var valueOrNull = StringComparer.OrdinalIgnoreCase.Equals(value, "null") ? null : value;
            Program.Configuration = Program.Configuration.Set(option, valueOrNull);
        }
    }

    private static class GetCommand
    {
        public static readonly Argument<string> Option = new Argument<string>(
                "option", "The option to modify.")
            .AddCompletions(Configuration.OptionNames)
            .AddValidatorEx(ValidateOption);

        public static Command SubCommand { get; } = BuildSubCommand();

        private static Command BuildSubCommand()
        {
            var cmd = new Command("get", "Print the value of an option.")
            {
                Option
            };
            cmd.SetHandler(Invoke, Option);
            return cmd;
        }

        public static void Invoke(string option)
        {
            Std.Out.WriteLine(Program.Configuration.Get(option) ?? "null");
        }
    }
}

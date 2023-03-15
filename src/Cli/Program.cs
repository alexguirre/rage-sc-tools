namespace ScTools.Cli;

using System.CommandLine;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ScTools.Cli.Commands;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        var rootCmd = new RootCommand("Tool for working with Grand Theft Auto V script files (.ysc).");
        rootCmd.AddCommand(CompileCommand.Command);
        rootCmd.AddCommand(DumpCommand.Command);
        return await rootCmd.InvokeAsync(args);
    }
}

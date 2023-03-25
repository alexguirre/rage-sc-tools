namespace ScTools.Cli;

using System.IO;
using Spectre.Console;

/// <summary>
/// Provides access to the standard output and error consoles.
/// </summary>
public static class Std
{
    private static readonly System.Lazy<IAnsiConsole> err = new(() => AnsiConsole.Create(new()
    {
        Out = new AnsiConsoleOutput(System.Console.Error),
    }));

    /// <summary>
    /// Gets the standard output console.
    /// </summary>
    public static IAnsiConsole Out => AnsiConsole.Console;
    /// <summary>
    /// Gets the standard output stream.
    /// </summary>
    public static TextWriter OutWriter => System.Console.Out;

    /// <summary>
    /// Gets the standard error console.
    /// </summary>
    public static IAnsiConsole Err => err.Value;
    /// <summary>
    /// Gets the standard error stream.
    /// </summary>
    public static TextWriter ErrWriter => System.Console.Error;


    public static void WriteDiagnostics(this IAnsiConsole console, DiagnosticsReport diagnostics, string indent = "  ")
    {
        foreach (var d in diagnostics.GetDiagnosticsSorted())
        {
            console.MarkupLineInterpolated($"[{(d.Severity is DiagnosticSeverity.Error ? "red" : "yellow")}]{indent}{d}[/]");
        }
    }
}

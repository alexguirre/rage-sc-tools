namespace ScTools.Cli;

using Spectre.Console;

internal static class Exit
{
    public static int Error(string message, int errorCode = DefaultError)
    {
        Std.Err.MarkupLineInterpolated($"[red]{message}[/]");
        return errorCode;
    }

    public const int DefaultError = 1;
    public const int Success = 0;
}

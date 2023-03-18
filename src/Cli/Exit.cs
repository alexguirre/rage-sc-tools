namespace ScTools.Cli;

using System;

internal static class Exit
{
    public static int Error(string message, int errorCode = DefaultError)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ForegroundColor = ConsoleColor.White;
        return errorCode;
    }

    public const int DefaultError = 1;
    public const int Success = 0;
}

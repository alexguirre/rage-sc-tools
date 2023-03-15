namespace ScTools.Cli;

using System;
using System.CommandLine.Parsing;
using System.Linq;
using ScriptLang.Workspace;

public class Parsers
{
    public static readonly ParseArgument<FileGlob[]> ParseFileGlobs = (ArgumentResult result) =>
        result.Tokens.Select(t => new FileGlob(t.Value)).ToArray();

    public static readonly ParseArgument<BuildTarget> ParseBuildTarget = (ArgumentResult result) =>
    {
        var parts = result.Tokens[0].Value.Split('-');
        return new(Enum.Parse<Game>(parts[0], ignoreCase: true), Enum.Parse<Platform>(parts[1], ignoreCase: true));
    };
}

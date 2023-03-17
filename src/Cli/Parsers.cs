namespace ScTools.Cli;

using System;
using System.CommandLine.Parsing;
using System.Linq;
using ScriptLang.Workspace;

public class Parsers
{
    public static readonly ParseArgument<FileGlob[]> ParseFileGlobs = result =>
        result.Tokens.Select(t => new FileGlob(t.Value)).ToArray();

    public static readonly ParseArgument<BuildTarget> ParseBuildTarget = result =>
    {
        if (BuildTarget.TryParse(result.Tokens[0].Value, out var target))
        {
            return target;
        }
        
        result.ErrorMessage = $"Invalid target '{result.Tokens[0].Value}'";
        return default;
    };
}

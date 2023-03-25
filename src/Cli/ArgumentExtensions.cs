namespace ScTools.Cli;

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;


public static class ArgumentExtensions
{
    // Just so we can chain calls
    public static TArgument AddValidatorEx<TArgument>(this TArgument argument, ValidateSymbolResult<ArgumentResult> validate)
        where TArgument : Argument
    {
        argument.AddValidator(validate);
        return argument;
    }


    public static Argument<FileGlob> AtLeastOne(this Argument<FileGlob> argument)
    {
        argument.AddValidator(static result => result.ErrorMessage =
                                  result.Tokens
                                        .Select(t => t.Value)
                                        .Where(glob => !new FileGlob(glob).HasMatches)
                                        .Select(LocalizationResources.Instance.FileDoesNotExist)
                                        .FirstOrDefault());

        return argument;
    }

    public static Argument<FileGlob[]> AtLeastOne(this Argument<FileGlob[]> argument)
    {
        argument.Arity = ArgumentArity.OneOrMore;
        argument.AddValidator(static result => result.ErrorMessage =
                                  new FileGlob(result.Tokens.Select(t => t.Value)).HasMatches ?
                                        null :
                                        LocalizationResources.Instance.FileDoesNotExist(string.Join(", ", result.Tokens.Select(t => t.Value))));

        return argument;
    }
}

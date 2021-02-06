namespace ScTools.Cli
{
    using System.CommandLine;
    using System.Linq;

    public static class ArgumentExtensions
    {
        public static Argument<FileGlob> AtLeastOne(this Argument<FileGlob> argument)
        {
            argument.AddValidator(symbol =>
                                      symbol.Tokens
                                            .Select(t => t.Value)
                                            .Where(glob => !new FileGlob(glob).HasMatches)
                                            .Select(ValidationMessages.Instance.FileDoesNotExist)
                                            .FirstOrDefault());

            return argument;
        }

        public static Argument<FileGlob[]> AtLeastOne(this Argument<FileGlob[]> argument)
        {
            argument.Arity = ArgumentArity.OneOrMore;
            argument.AddValidator(symbol =>
                                      new FileGlob(symbol.Tokens.Select(t => t.Value)).HasMatches ?
                                            null :
                                            ValidationMessages.Instance.FileDoesNotExist(string.Join(", ", symbol.Tokens.Select(t => t.Value))));

            return argument;
        }
    }
}

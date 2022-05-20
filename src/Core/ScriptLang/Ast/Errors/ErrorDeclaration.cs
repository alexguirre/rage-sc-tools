namespace ScTools.ScriptLang.Ast.Errors;

using ScTools.ScriptLang.Ast.Declarations;

public sealed partial class ErrorDeclaration : BaseError, IDeclaration
{
    public string Name => "#ERROR#";
    public Token NameToken => Token.Identifier(Name, Location);

    public ErrorDeclaration(Diagnostic diagnostic, params Token[] tokens)
        : base(diagnostic, OfTokens(tokens), OfChildren())
    {
    }
}

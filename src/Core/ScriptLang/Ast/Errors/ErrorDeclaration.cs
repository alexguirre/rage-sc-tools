namespace ScTools.ScriptLang.Ast.Errors;

using ScTools.ScriptLang.Ast.Declarations;

public sealed class ErrorDeclaration : BaseError, IDeclaration
{
    public string Name => "#ERROR#";
    public Token NameToken => Token.Identifier(Name, Location);

    public ErrorDeclaration(Diagnostic diagnostic, params Token[] tokens)
        : base(diagnostic, OfTokens(tokens), OfChildren())
    {
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);
}

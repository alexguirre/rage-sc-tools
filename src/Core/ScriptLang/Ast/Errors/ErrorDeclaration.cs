namespace ScTools.ScriptLang.Ast.Errors;

using ScTools.ScriptLang.Ast.Declarations;

public sealed class ErrorDeclaration : BaseError, IDeclaration
{
    public string Name => "#ERROR#";

    public ErrorDeclaration(Diagnostic diagnostic, params Token[] tokens)
        : base(diagnostic, OfTokens(tokens), OfChildren())
    {
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

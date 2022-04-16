namespace ScTools.ScriptLang.Ast.Errors;

using ScTools.ScriptLang.Ast.Declarations;

public sealed class ErrorDeclaration_New : BaseError, IDeclaration_New
{
    public string Name => "#ERROR#";

    public ErrorDeclaration_New(Diagnostic diagnostic, params Token[] tokens)
        : base(diagnostic, OfTokens(tokens), OfChildren())
    {
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

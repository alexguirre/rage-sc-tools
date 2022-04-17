namespace ScTools.ScriptLang.Ast.Errors;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class ErrorExpression : BaseError, IExpression
{
    public ExpressionSemantics Semantics { get; set; }

    public ErrorExpression(Diagnostic diagnostic, params Token[] tokens) : base(diagnostic, OfTokens(tokens), OfChildren()) { }
    public ErrorExpression(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source, diagnostics, message) { }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);
}

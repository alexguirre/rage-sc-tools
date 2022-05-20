namespace ScTools.ScriptLang.Ast.Errors;

using ScTools.ScriptLang.Ast.Expressions;

public sealed partial class ErrorExpression : BaseError, IExpression
{
    public ExpressionSemantics Semantics { get; set; }

    public ErrorExpression(Diagnostic diagnostic, params Token[] tokens) : base(diagnostic, OfTokens(tokens), OfChildren()) { }
    public ErrorExpression(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source, diagnostics, message) { }
}

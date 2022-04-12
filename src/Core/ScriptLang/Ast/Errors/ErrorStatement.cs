namespace ScTools.ScriptLang.Ast.Errors;

using ScTools.ScriptLang.Ast.Statements;

public sealed class ErrorStatement : BaseError, IStatement, IBreakableStatement, ILoopStatement
{
    public string? Label { get; set; }
    public string? ExitLabel { get; set; }
    public string? BeginLabel { get; set; }
    public string? ContinueLabel { get; set; }

    public ErrorStatement(Diagnostic diagnostic, params Token[] tokens) : base(diagnostic, tokens) { }
    public ErrorStatement(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source, diagnostics, message) { }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

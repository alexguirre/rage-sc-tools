namespace ScTools.ScriptLang.Ast.Errors;

using ScTools.ScriptLang.Ast.Statements;

public sealed class ErrorStatement : BaseError, IStatement, IBreakableStatement, ILoopStatement
{
    public string? Label { get; set; }
    public LoopStatementSemantics Semantics { get; set; }
    BreakableStatementSemantics ISemanticNode<BreakableStatementSemantics>.Semantics
    {
        get => new(Semantics.ExitLabel);
        set => Semantics = Semantics with { ExitLabel = value.ExitLabel };
    }

    public ErrorStatement(Diagnostic diagnostic, params Token[] tokens) : base(diagnostic, tokens) { }
    public ErrorStatement(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source, diagnostics, message) { }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

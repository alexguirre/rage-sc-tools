namespace ScTools.ScriptLang.Ast.Errors;

using ScTools.ScriptLang.Ast.Statements;

public sealed class ErrorStatement : BaseError, IStatement, IBreakableStatement, ILoopStatement
{
    public Label? Label { get; }
    public LoopStatementSemantics Semantics { get; set; }
    BreakableStatementSemantics ISemanticNode<BreakableStatementSemantics>.Semantics
    {
        get => new(Semantics.ExitLabel);
        set => Semantics = Semantics with { ExitLabel = value.ExitLabel };
    }

    public ErrorStatement(Diagnostic diagnostic, Label? label, params Token[] tokens)
        : base(diagnostic, OfTokens(tokens), OfChildren().AppendIfNotNull(label))
    {
        Label = label;
    }
    public ErrorStatement(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source, diagnostics, message) { }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);
}

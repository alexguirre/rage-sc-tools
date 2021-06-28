namespace ScTools.ScriptLang.Ast.Errors
{
    using ScTools.ScriptLang.Ast.Statements;

    public sealed class ErrorStatement : BaseError, IStatement, IBreakableStatement, ILoopStatement
    {
        public string? ExitLabel { get; set; }
        public string? BeginLabel { get; set; }
        public string? ContinueLabel { get; set; }

        public ErrorStatement(SourceRange source, Diagnostic diagnostic) : base(source, diagnostic) { }
        public ErrorStatement(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source, diagnostics, message) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

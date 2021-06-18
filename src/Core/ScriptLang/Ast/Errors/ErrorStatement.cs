namespace ScTools.ScriptLang.Ast.Errors
{
    using ScTools.ScriptLang.Ast.Statements;

    public sealed class ErrorStatement : BaseError, IStatement, IBreakableStatement
    {
        public ErrorStatement(SourceRange source, Diagnostic diagnostic) : base(source, diagnostic) { }
        public ErrorStatement(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source, diagnostics, message) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

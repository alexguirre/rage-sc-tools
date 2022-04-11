namespace ScTools.ScriptLang.Ast.Errors
{
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;

    public sealed class ErrorExpression : BaseError, IExpression
    {
        public IType? Type { get; set; }
        public bool IsLValue { get; set; }
        public bool IsConstant { get; set; }

        public ErrorExpression(Diagnostic diagnostic, params Token[] tokens) : base(diagnostic, tokens) { }
        public ErrorExpression(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source, diagnostics, message) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

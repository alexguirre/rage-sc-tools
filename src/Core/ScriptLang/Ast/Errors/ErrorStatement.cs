namespace ScTools.ScriptLang.Ast.Errors
{
    using ScTools.ScriptLang.Ast.Statements;

    public sealed class ErrorStatement : BaseStatement, IBreakableStatement, IError
    {
        public ErrorStatement(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

namespace ScTools.ScriptLang.Ast.Statements
{
    public sealed class BreakStatement : BaseStatement
    {
        public BreakStatement(SourceRange source) : base(source) {}

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

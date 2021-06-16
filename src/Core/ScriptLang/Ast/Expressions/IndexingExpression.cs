namespace ScTools.ScriptLang.Ast.Expressions
{
    public sealed class IndexingExpression : BaseExpression
    {
        public IExpression Array { get; set; }
        public IExpression Index { get; set; }

        public IndexingExpression(SourceRange source, IExpression array, IExpression index) : base(source)
            => (Array, Index) = (array, index);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

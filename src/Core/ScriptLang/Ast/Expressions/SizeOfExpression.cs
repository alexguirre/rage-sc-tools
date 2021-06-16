namespace ScTools.ScriptLang.Ast.Expressions
{
    public sealed class SizeOfExpression : BaseExpression
    {
        public IExpression SubExpression { get; set; }

        public SizeOfExpression(SourceRange source, IExpression subExpression) : base(source)
            => SubExpression = subExpression;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

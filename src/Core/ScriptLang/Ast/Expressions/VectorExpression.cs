namespace ScTools.ScriptLang.Ast.Expressions
{
    public sealed class VectorExpression : BaseExpression
    {
        public IExpression X { get; set; }
        public IExpression Y { get; set; }
        public IExpression Z { get; set; }

        public VectorExpression(SourceRange source, IExpression x, IExpression y, IExpression z) : base(source)
            => (X, Y, Z) = (x, y, z);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

namespace ScTools.ScriptLang.Ast.Expressions
{
    public enum UnaryOperator
    {
        Negate,
        LogicalNot,
    }

    public sealed class UnaryExpression : BaseExpression
    {
        public UnaryOperator Operator { get; set; }
        public IExpression SubExpression { get; set; }

        public UnaryExpression(SourceRange source, UnaryOperator op, IExpression subExpression) : base(source)
            => (Operator, SubExpression) = (op, subExpression);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

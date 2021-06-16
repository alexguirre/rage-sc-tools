namespace ScTools.ScriptLang.Ast.Expressions
{
    public sealed class NullExpression : BaseExpression
    {
        public NullExpression(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

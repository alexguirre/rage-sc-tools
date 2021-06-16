namespace ScTools.ScriptLang.Ast.Expressions
{
    public sealed class FloatLiteralExpression : BaseExpression
    {
        public float Value { get; set; }

        public FloatLiteralExpression(SourceRange source, float value) : base(source)
            => Value = value;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

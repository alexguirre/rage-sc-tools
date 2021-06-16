namespace ScTools.ScriptLang.Ast.Expressions
{
    public sealed class BoolLiteralExpression : BaseExpression
    {
        public bool Value { get; set; }

        public BoolLiteralExpression(SourceRange source, bool value) : base(source)
            => Value = value;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

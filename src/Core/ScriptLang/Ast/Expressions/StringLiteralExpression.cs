namespace ScTools.ScriptLang.Ast.Expressions
{
    public sealed class StringLiteralExpression : BaseExpression
    {
        public string Value { get; set; }

        public StringLiteralExpression(SourceRange source, string value) : base(source)
            => Value = value;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

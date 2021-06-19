namespace ScTools.ScriptLang.Ast.Expressions
{
    /// <remarks>
    /// The string is nullable for representing the value of <code>STRING s = NULL</code> as a literal.
    /// </remarks> 
    public sealed class StringLiteralExpression : BaseExpression, ILiteralExpression<string?>
    {
        public string? Value { get; set; }

        public StringLiteralExpression(SourceRange source, string? value) : base(source)
            => Value = value;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        object? ILiteralExpression.Value => Value;
    }
}

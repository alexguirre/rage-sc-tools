namespace ScTools.ScriptLang.Ast.Expressions
{
    using ScTools.ScriptLang.Ast.Declarations;

    /// <summary>
    /// Represents a reference to a <see cref="IValueDeclaration"/>.
    /// </summary>
    public sealed class ValueDeclRefExpression : BaseExpression
    {
        public string Name { get; set; }
        public IValueDeclaration? Declaration { get; set; }

        public ValueDeclRefExpression(SourceRange source, string name) : base(source)
            => Name = name;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

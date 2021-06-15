namespace ScTools.ScriptLang.Ast.Statements
{
    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class ReturnStatement : BaseStatement
    {
        public IExpression? Expression { get; set; }

        public ReturnStatement(SourceRange source, IExpression? expression) : base(source)
            => Expression = expression;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

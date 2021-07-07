namespace ScTools.ScriptLang.Ast.Statements
{
    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class AssignmentStatement : BaseStatement
    {
        public IExpression LHS { get; set; }
        public IExpression RHS { get; set; }

        public AssignmentStatement(SourceRange source, IExpression lhs, IExpression rhs) : base(source)
            => (LHS, RHS) = (lhs, rhs);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

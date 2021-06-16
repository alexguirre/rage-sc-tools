namespace ScTools.ScriptLang.Ast.Statements
{
    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class AssignmentStatement : BaseStatement
    {
        public BinaryOperator? CompoundOperator { get; set; }
        public IExpression LHS { get; set; }
        public IExpression RHS { get; set; }

        public AssignmentStatement(SourceRange source, BinaryOperator? compoundOperator, IExpression lhs, IExpression rhs) : base(source)
            => (CompoundOperator, LHS, RHS) = (compoundOperator, lhs, rhs);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

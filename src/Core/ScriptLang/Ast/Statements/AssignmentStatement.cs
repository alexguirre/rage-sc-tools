namespace ScTools.ScriptLang.Ast.Statements
{
    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class AssignmentStatement : BaseStatement
    {
        /// <summary>
        /// Gets or sets the operator if this is a compound assignment; <c>null</c> if it is a simple assigment.
        /// </summary>
        public BinaryOperator? CompoundOperator { get; set; }
        public IExpression LHS { get; set; }
        public IExpression RHS { get; set; }
        public BinaryExpression? CompoundExpression { get; set; }

        public AssignmentStatement(SourceRange source, BinaryOperator? compoundOperator, IExpression lhs, IExpression rhs) : base(source)
            => (CompoundOperator, LHS, RHS) = (compoundOperator, lhs, rhs);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

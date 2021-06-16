namespace ScTools.ScriptLang.Ast.Expressions
{
    public enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        And,
        Xor,
        Or,
        Equals,
        NotEquals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LogicalAnd,
        LogicalOr,
    }

    public sealed class BinaryExpression : BaseExpression
    {
        public BinaryOperator Operator { get; set; }
        public IExpression LHS { get; set; }
        public IExpression RHS { get; set; }

        public BinaryExpression(SourceRange source, BinaryOperator op, IExpression lhs, IExpression rhs) : base(source)
            => (Operator, LHS, RHS) = (op, lhs, rhs);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

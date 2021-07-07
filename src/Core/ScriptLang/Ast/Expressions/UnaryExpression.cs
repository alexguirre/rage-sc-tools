namespace ScTools.ScriptLang.Ast.Expressions
{
    using System;

    public enum UnaryOperator
    {
        Negate,
        LogicalNot,
    }

    public sealed class UnaryExpression : BaseExpression
    {
        public UnaryOperator Operator { get; set; }
        public IExpression SubExpression { get; set; }

        public UnaryExpression(SourceRange source, UnaryOperator op, IExpression subExpression) : base(source)
            => (Operator, SubExpression) = (op, subExpression);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }

    public static class UnaryOperatorExtensions
    {
        public static string ToToken(this UnaryOperator op)
            => op switch
            {
                UnaryOperator.Negate => "-",
                UnaryOperator.LogicalNot => "NOT",
                _ => throw new ArgumentOutOfRangeException(nameof(op))
            };

        public static UnaryOperator FromToken(string token)
            => token.ToUpperInvariant() switch
            {
                "-" => UnaryOperator.Negate,
                "NOT" => UnaryOperator.LogicalNot,
                _ => throw new ArgumentException($"Unknown unary operator '{token}'", nameof(token)),
            };
    }
}

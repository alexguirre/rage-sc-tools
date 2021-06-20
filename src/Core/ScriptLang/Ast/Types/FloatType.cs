namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class FloatType : BaseType
    {
        public override int SizeOf => 1;

        public FloatType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is FloatType;

        public override bool CanAssign(IType rhs) => rhs.ByValue is FloatType or NullType or ErrorType;

        public override IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
        {
            rhs = rhs.ByValue;
            if (rhs is ErrorType)
            {
                return rhs;
            }

            if (rhs is FloatType)
            {
                IType? ty = op switch
                {
                    BinaryOperator.Add or BinaryOperator.Subtract or
                    BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Modulo
                        => new FloatType(source),

                    BinaryOperator.Equals or BinaryOperator.NotEquals or
                    BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual or
                    BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual
                        => new BoolType(source),
                    _ => null,
                };

                if (ty is not null)
                {
                    return ty;
                }
            }

            return base.BinaryOperation(op, rhs, source, diagnostics);
        }

        public override IType UnaryOperation(UnaryOperator op, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (op is UnaryOperator.Negate)
            {
                return new FloatType(source);
            }

            return base.UnaryOperation(op, source, diagnostics);
        }
    }
}

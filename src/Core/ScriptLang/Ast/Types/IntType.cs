namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class IntType : BaseType
    {
        public override int SizeOf => 1;

        public IntType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is IntType;

        public override bool CanAssign(IType rhs) => rhs.ByValue is IntType or EnumType or NullType or ErrorType;

        public override IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
        {
            rhs = rhs.ByValue;
            if (rhs is ErrorType)
            {
                return rhs;
            }

            if (rhs is IntType or EnumType)
            {
                IType? ty = op switch
                {
                    BinaryOperator.Add or BinaryOperator.Subtract or
                    BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Modulo or
                    BinaryOperator.And or BinaryOperator.Xor or BinaryOperator.Or
                        => new IntType(source),

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
                return new IntType(source);
            }

            return base.UnaryOperation(op, source, diagnostics);
        }
    }
}

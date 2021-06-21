namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;

    public sealed class BoolType : BaseType
    {
        public override int SizeOf => 1;

        public BoolType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is BoolType;

        public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs.ByValue is BoolType or NullType or ErrorType;

        public override IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
        {
            rhs = rhs.ByValue;
            if (rhs is ErrorType)
            {
                return rhs;
            }

            if (rhs is BoolType)
            {
                IType? ty = op switch
                {
                    BinaryOperator.LogicalAnd or BinaryOperator.LogicalOr
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
            if (op is UnaryOperator.LogicalNot)
            {
                return new BoolType(source);
            }

            return base.UnaryOperation(op, source, diagnostics);
        }
    }
}

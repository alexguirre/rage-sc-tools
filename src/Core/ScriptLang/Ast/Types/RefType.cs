using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;

namespace ScTools.ScriptLang.Ast.Types
{
    public sealed class RefType : BaseType
    {
        public IType PointeeType { get; set; }

        public override int SizeOf => 1;
        public override IType ByValue => PointeeType;

        public RefType(SourceRange source, IType pointeeType) : base(source)
            => PointeeType = pointeeType;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is RefType otherRef && PointeeType.Equivalent(otherRef.PointeeType);

        public override bool CanAssign(IType rhs, bool rhsIsLValue) => PointeeType.CanAssign(rhs, rhsIsLValue);
        public override bool CanAssignInit(IType rhs, bool rhsIsLValue) => Equivalent(rhs) || (rhsIsLValue && PointeeType.CanBindRefTo(rhs.ByValue));

        public override void Assign(IType rhs, bool rhsIsLValue, SourceRange source, DiagnosticsReport diagnostics) => PointeeType.Assign(rhs, rhsIsLValue, source, diagnostics);

        public override void AssignInit(IType rhs, bool rhsIsLValue, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (Equivalent(rhs))
            {
                return;
            }

            if (!rhsIsLValue)
            {
                diagnostics.AddError($"Cannot bind reference of type '{this}' to non-lvalue", source);
            }
            else if (!PointeeType.CanBindRefTo(rhs.ByValue))
            {
                diagnostics.AddError($"Cannot bind reference of type '{this}' to type '{rhs}'", source);
            }
        }

        public override IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
            => PointeeType.BinaryOperation(op, rhs, source, diagnostics);

        public override IType UnaryOperation(UnaryOperator op, SourceRange source, DiagnosticsReport diagnostics)
            => PointeeType.UnaryOperation(op, source, diagnostics);

        public override (IType Type, bool LValue) FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics)
            => PointeeType.FieldAccess(fieldName, source, diagnostics);

        public override IType Indexing(IType index, SourceRange source, DiagnosticsReport diagnostics)
            => PointeeType.Indexing(index, source, diagnostics);

        public override IType Invocation((IType Type, bool IsLValue, SourceRange Source)[] args, SourceRange source, DiagnosticsReport diagnostics)
            => PointeeType.Invocation(args, source, diagnostics);

        public override void CGBinaryOperation(CodeGenerator cg, BinaryExpression expr)
            => PointeeType.CGBinaryOperation(cg, expr);

        public override void CGUnaryOperation(CodeGenerator cg, UnaryExpression expr)
            => PointeeType.CGUnaryOperation(cg, expr);
    }
}

namespace ScTools.ScriptLang.Ast.Types
{
    using System;
    using System.Linq;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.BuiltIns;
    using ScTools.ScriptLang.CodeGen;

    public sealed class StructType : BaseType
    {
        public override int SizeOf => Declaration.Fields.Sum(f => f.Type.SizeOf);
        public StructDeclaration Declaration { get; set; }

        public StructType(SourceRange source, StructDeclaration declaration) : base(source)
            => Declaration = declaration;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is StructType otherStruct && otherStruct.Declaration == Declaration;

        public override bool CanAssign(IType rhs, bool rhsIsLValue)
        {
            rhs = rhs.ByValue;
            if (rhs is ErrorType || Equivalent(rhs))
            {
                return true;
            }

            if (BuiltInTypes.IsHandleType(this))
            {
                // special case for built-in handle-like structs (e.g. ENTITY_INDEX, PED_INDEX, VEHICLE_INDEX, ...)

                // allow to assign NULL, e.g: PED_INDEX myPed = NULL
                if (rhs is NullType)
                {
                    return true;
                }

                // allow to assign PED/VEHICLE/OBJECT_INDEX to ENTITY_INDEX (to simplify native calls that expect ENTITY_INDEX but you have some other handle type)
                if (Declaration == BuiltInTypes.EntityIndex && BuiltInTypes.IsHandleType(rhs))
                {
                    var rhsDecl = ((StructType)rhs).Declaration;
                    return rhsDecl == BuiltInTypes.PedIndex ||
                           rhsDecl == BuiltInTypes.VehicleIndex ||
                           rhsDecl == BuiltInTypes.ObjectIndex;
                }
            }

            return false;
        }

        public override (IType Type, bool LValue) FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics)
        {
            var field = Declaration.FindField(fieldName);
            if (field is null)
            {
                return (new ErrorType(source, diagnostics, $"Unknown field '{fieldName}'"), true);
            }
            else
            {
                return (field.Type, true);
            }
        }

        public override IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (rhs is ErrorType)
            {
                return rhs;
            }

            if (op is BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide && 
                BuiltInTypes.IsVectorType(this) && BuiltInTypes.IsVectorType(rhs))
            {
                // special case to allow +-*/ operations for VECTOR type
                return new StructType(source, Declaration);
            }

            return base.BinaryOperation(op, rhs, source, diagnostics);
        }

        public override IType UnaryOperation(UnaryOperator op, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (op is UnaryOperator.Negate && BuiltInTypes.IsVectorType(this))
            {
                // special case to allow negation for VECTOR type
                return new StructType(source, Declaration);
            }

            return base.UnaryOperation(op, source, diagnostics);
        }

        public override void CGBinaryOperation(CodeGenerator cg, BinaryExpression expr)
        {
            if (BuiltInTypes.IsVectorType(this))
            {
                cg.EmitValue(expr.LHS);
                cg.EmitValue(expr.RHS);
                switch (expr.Operator)
                {
                    case BinaryOperator.Add: cg.Emit(Opcode.VADD); break;
                    case BinaryOperator.Subtract: cg.Emit(Opcode.VSUB); break;
                    case BinaryOperator.Multiply: cg.Emit(Opcode.VMUL); break;
                    case BinaryOperator.Divide: cg.Emit(Opcode.VDIV); break;

                    default: throw new NotImplementedException();
                }
                return;
            }

            throw new NotImplementedException();
        }

        public override void CGUnaryOperation(CodeGenerator cg, UnaryExpression expr)
        {
            if (expr.Operator is UnaryOperator.Negate && BuiltInTypes.IsVectorType(this))
            {
                cg.EmitValue(expr.SubExpression);
                cg.Emit(Opcode.VNEG);
                return;
            }

            throw new NotImplementedException();
        }
    }
}

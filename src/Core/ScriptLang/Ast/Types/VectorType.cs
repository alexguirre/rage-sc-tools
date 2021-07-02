namespace ScTools.ScriptLang.Ast.Types
{
    using System;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.CodeGen;

    public sealed class VectorType : BaseType
    {
        public override int SizeOf => 3;

        public VectorType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is VectorType;

        public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs is ErrorType || Equivalent(rhs);

        public override IType FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (Parser.CaseInsensitiveComparer.Equals(fieldName, "x") ||
                Parser.CaseInsensitiveComparer.Equals(fieldName, "y") ||
                Parser.CaseInsensitiveComparer.Equals(fieldName, "z"))
            {
                return new FloatType(source);
            }
            else
            {
                return new ErrorType(source, diagnostics, $"Unknown field '{fieldName}'");
            }
        }

        public override IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (rhs is ErrorType)
            {
                return rhs;
            }

            if (rhs is VectorType &&
                op is BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide)
            {
                return new VectorType(source);
            }

            return base.BinaryOperation(op, rhs, source, diagnostics);
        }

        public override IType UnaryOperation(UnaryOperator op, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (op is UnaryOperator.Negate)
            {
                return new VectorType(source);
            }

            return base.UnaryOperation(op, source, diagnostics);
        }

        public override void CGBinaryOperation(CodeGenerator cg, BinaryExpression expr)
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
        }

        public override void CGUnaryOperation(CodeGenerator cg, UnaryExpression expr)
        {
            cg.EmitValue(expr.SubExpression);
            switch (expr.Operator)
            {
                case UnaryOperator.Negate: cg.Emit(Opcode.VNEG); break;

                default: throw new NotImplementedException();
            }
        }

        public override void CGFieldAddress(CodeGenerator cg, FieldAccessExpression expr)
        {
            cg.EmitAddress(expr.SubExpression);
            cg.EmitOffset(OffsetOfField(expr.FieldName));
        }

        private static int OffsetOfField(string fieldName)
        {
            if (Parser.CaseInsensitiveComparer.Equals(fieldName, "x"))
            {
                return 0;
            }
            else if (Parser.CaseInsensitiveComparer.Equals(fieldName, "y"))
            {
                return 1;
            }
            else if (Parser.CaseInsensitiveComparer.Equals(fieldName, "z"))
            {
                return 2;
            }
            else
            {
                throw new ArgumentException($"Invalid field '{fieldName}' for VECTOR");
            }
        }
    }
}

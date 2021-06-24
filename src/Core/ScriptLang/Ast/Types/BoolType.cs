namespace ScTools.ScriptLang.Ast.Types
{
    using System;
    using System.Diagnostics;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.CodeGen;

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

        public override void CGBinaryOperation(CodeGenerator cg, BinaryExpression expr)
        {
            switch (expr.Operator)
            {
                case BinaryOperator.LogicalAnd:
                {
                    Debug.Assert(expr.ShortCircuitLabel is not null);
                    cg.EmitValue(expr.LHS);
                    cg.Emit(Opcode.DUP);
                    cg.EmitJumpIfZero(expr.ShortCircuitLabel);
                    cg.EmitValue(expr.RHS);
                    cg.Emit(Opcode.IAND);
                    cg.EmitLabel(expr.ShortCircuitLabel);
                }
                break;
                case BinaryOperator.LogicalOr:
                {
                    Debug.Assert(expr.ShortCircuitLabel is not null);
                    cg.EmitValue(expr.LHS);
                    cg.Emit(Opcode.DUP);
                    cg.Emit(Opcode.INOT);
                    cg.EmitJumpIfZero(expr.ShortCircuitLabel);
                    cg.EmitValue(expr.RHS);
                    cg.Emit(Opcode.IOR);
                    cg.EmitLabel(expr.ShortCircuitLabel);
                }
                break;

                default: throw new NotImplementedException();
            }
        }

        public override void CGUnaryOperation(CodeGenerator cg, UnaryExpression expr)
        {
            cg.EmitValue(expr.SubExpression);
            switch (expr.Operator)
            {
                case UnaryOperator.LogicalNot: cg.Emit(Opcode.INOT); break;

                default: throw new NotImplementedException();
            }
        }
    }
}

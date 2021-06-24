namespace ScTools.ScriptLang.Ast.Types
{
    using System;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.CodeGen;

    public sealed class EnumType : BaseType
    {
        public override int SizeOf => 1;
        public EnumDeclaration Declaration { get; set; }

        public EnumType(SourceRange source, EnumDeclaration declaration) : base(source)
            => Declaration = declaration;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is EnumType otherEnum && otherEnum.Declaration == Declaration;

        public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs.ByValue is IntType or NullType or ErrorType || Equivalent(rhs.ByValue);

        public override IType BinaryOperation(BinaryOperator op, IType rhs, SourceRange source, DiagnosticsReport diagnostics)
        {
            rhs = rhs.ByValue;
            if (rhs is ErrorType)
            {
                return rhs;
            }

            if (rhs is IntType || (rhs is EnumType rhsEnumTy && rhsEnumTy.Declaration == Declaration))
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

        public override void CGBinaryOperation(CodeGenerator cg, BinaryExpression expr)
        {
            cg.EmitValue(expr.LHS);
            cg.EmitValue(expr.RHS);
            switch (expr.Operator)
            {
                case BinaryOperator.Add: cg.Emit(Opcode.IADD); break;
                case BinaryOperator.Subtract: cg.Emit(Opcode.ISUB); break;
                case BinaryOperator.Multiply: cg.Emit(Opcode.IMUL); break;
                case BinaryOperator.Divide: cg.Emit(Opcode.IDIV); break;
                case BinaryOperator.Modulo: cg.Emit(Opcode.IMOD); break;
                case BinaryOperator.And: cg.Emit(Opcode.IAND); break;
                case BinaryOperator.Xor: cg.Emit(Opcode.IXOR); break;
                case BinaryOperator.Or: cg.Emit(Opcode.IOR); break;

                case BinaryOperator.Equals: cg.Emit(Opcode.IEQ); break;
                case BinaryOperator.NotEquals: cg.Emit(Opcode.INE); break;
                case BinaryOperator.LessThan: cg.Emit(Opcode.ILT); break;
                case BinaryOperator.LessThanOrEqual: cg.Emit(Opcode.ILE); break;
                case BinaryOperator.GreaterThan: cg.Emit(Opcode.IGT); break;
                case BinaryOperator.GreaterThanOrEqual: cg.Emit(Opcode.IGE); break;

                default: throw new NotImplementedException();
            }
        }
    }
}

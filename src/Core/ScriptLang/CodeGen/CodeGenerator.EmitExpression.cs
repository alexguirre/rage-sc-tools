#nullable enable
namespace ScTools.ScriptLang.CodeGen
{
    using System;
    using System.Diagnostics;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Semantics.Binding;

    public sealed partial class CodeGenerator
    {
        private void EmitExpression(BoundExpression expr)
        {
            switch (expr)
            {
                case BoundBinaryExpression e: EmitBinaryExpression(e); break;
                case BoundIntLiteralExpression e: EmitIntLiteralExpression(e); break;
                case BoundFloatLiteralExpression e: EmitFloatLiteralExpression(e); break;
                default: throw new NotImplementedException(expr.GetType().Name);
            }
        }

        private void EmitVariableExpression(BoundVariableExpression expr, bool assignable)
        {
            Debug.Assert(!assignable, "assignable not supported yet");
        }

        private void EmitBinaryExpression(BoundBinaryExpression expr)
        {
            EmitExpression(expr.Left);
            EmitExpression(expr.Right);

            if (expr.Left.Type is BasicType { TypeCode: BasicTypeCode.Int } &&
                expr.Left.Type == expr.Right.Type)
            {
                switch (expr.Op)
                {
                    case BinaryOperator.Add: Code.Emit(Opcode.IADD); break;
                    case BinaryOperator.Subtract: Code.Emit(Opcode.ISUB); break;
                    case BinaryOperator.Multiply: Code.Emit(Opcode.IMUL); break;
                    case BinaryOperator.Divide: Code.Emit(Opcode.IDIV); break;
                    case BinaryOperator.Modulo: Code.Emit(Opcode.IMOD); break;
                    case BinaryOperator.And: Code.Emit(Opcode.IAND); break;
                    case BinaryOperator.Or: Code.Emit(Opcode.IOR); break;
                    case BinaryOperator.Xor: Code.Emit(Opcode.IXOR); break;
                    default: throw new NotImplementedException(expr.Op.ToString());
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void EmitIntLiteralExpression(BoundIntLiteralExpression expr)
            => EmitPushInt(expr.Value);

        private void EmitFloatLiteralExpression(BoundFloatLiteralExpression expr)
            => EmitPushFloat(expr.Value);
    }
}

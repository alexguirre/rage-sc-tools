#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics.Symbols;

    using Opcode = ScTools.ScriptAssembly.Opcode;
    using Type = ScTools.ScriptLang.Semantics.Type;

    public abstract class BoundExpression : BoundNode
    {
        public Type? Type { get; protected set; }
        /// <summary>
        /// Gets whether the value of this expression is known at compile time.
        /// </summary>
        public abstract bool IsConstant { get; }
        /// <summary>
        /// Gets whether <see cref="EmitAddr(ByteCodeBuilder)"/> is supported.
        /// </summary>
        public abstract bool IsAddressable { get; }
        /// <summary>
        /// Gets whether this expression or any of its sub-expressions are <see cref="BoundInvalidExpression"/>.
        /// </summary>
        public abstract bool IsInvalid { get; }

        public abstract void EmitLoad(ByteCodeBuilder code);
        public abstract void EmitStore(ByteCodeBuilder code);
        public abstract void EmitAddr(ByteCodeBuilder code);
        public abstract void EmitCall(ByteCodeBuilder code);
    }

    public class BoundInvalidExpression : BoundExpression
    {
        public override bool IsConstant => false;
        public override bool IsAddressable => false;
        public override bool IsInvalid => true;
        public string Reason { get; }

        public BoundInvalidExpression(string reason)
        {
            Reason = reason;
            Type = null;
        }

        public override void EmitLoad(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitAddr(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundUnknownSymbolExpression : BoundInvalidExpression
    {
        public string Identifier { get; }

        public BoundUnknownSymbolExpression(string identifier) : base($"Unknown symbol '{identifier}'")
        {
            Identifier = identifier;
        }
    }

    public sealed class BoundUnknownMemberAccessExpression : BoundInvalidExpression
    {
        public BoundExpression Expression { get; }
        public string Member { get; }

        public BoundUnknownMemberAccessExpression(BoundExpression expression, string member) : base($"Unknown member '{member}'")
        {
            Expression = expression;
            Member = member;
        }
    }

    public sealed class BoundUnaryExpression : BoundExpression
    {
        public override bool IsConstant => Operand.IsConstant;
        public override bool IsAddressable => false;
        public override bool IsInvalid => Operand.IsInvalid;
        public BoundExpression Operand { get; }
        public Ast.UnaryOperator Op { get; }

        public BoundUnaryExpression(BoundExpression operand, Ast.UnaryOperator op)
        {
            Operand = operand;
            Op = op;
            Type = Operand.Type?.UnderlyingType;
        }

        public override void EmitLoad(ByteCodeBuilder code)
        {
            var operandType = Type;
            if (operandType is BasicType { TypeCode: BasicTypeCode.Float } ||
                operandType is BasicType { TypeCode: BasicTypeCode.Int })
            {
                Operand.EmitLoad(code);
                var isFloat = Type is BasicType { TypeCode: BasicTypeCode.Float };
                switch (Op)
                {
                    case Ast.UnaryOperator.Negate:
                        code.Emit(isFloat ? Opcode.FNEG : Opcode.INEG);
                        break;
                    default: throw new NotImplementedException();
                }
            }
            else if (operandType is BasicType { TypeCode: BasicTypeCode.Bool })
            {
                Operand.EmitLoad(code);
                switch (Op)
                {
                    case Ast.UnaryOperator.Not:
                        code.Emit(Opcode.INOT);
                        break;
                    default: throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitAddr(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundBinaryExpression : BoundExpression
    {
        public override bool IsConstant => Left.IsConstant && Right.IsConstant;
        public override bool IsAddressable => false;
        public override bool IsInvalid => Left.IsInvalid || Right.IsInvalid;
        public BoundExpression Left { get; }
        public BoundExpression Right { get; }
        public Ast.BinaryOperator Op { get; }

        public BoundBinaryExpression(BoundExpression left, BoundExpression right, Ast.BinaryOperator op)
        {
            Left = left;
            Right = right;
            Op = op;

            if (!left.IsInvalid && !right.IsInvalid)
            {
                Debug.Assert(left.Type?.UnderlyingType == right.Type?.UnderlyingType);
                Type = Ast.BinaryExpression.OpIsComparison(op) ? new BasicType(BasicTypeCode.Bool) : left.Type?.UnderlyingType;
            }
        }

        public override void EmitLoad(ByteCodeBuilder code)
        {
            var operandsType = Left.Type?.UnderlyingType;
            if (operandsType is BasicType { TypeCode: BasicTypeCode.Float } ||
                operandsType is BasicType { TypeCode: BasicTypeCode.Int })
            {
                Left.EmitLoad(code);
                Right.EmitLoad(code);
                var isFloat = operandsType is BasicType { TypeCode: BasicTypeCode.Float };
                switch (Op)
                {
                    case Ast.BinaryOperator.Add:
                        code.Emit(isFloat ? Opcode.FADD : Opcode.IADD);
                        break;
                    case Ast.BinaryOperator.Subtract:
                        code.Emit(isFloat ? Opcode.FSUB : Opcode.ISUB);
                        break;
                    case Ast.BinaryOperator.Multiply:
                        code.Emit(isFloat ? Opcode.FMUL : Opcode.IMUL);
                        break;
                    case Ast.BinaryOperator.Divide:
                        code.Emit(isFloat ? Opcode.FDIV : Opcode.IDIV);
                        break;
                    case Ast.BinaryOperator.Modulo:
                        code.Emit(isFloat ? Opcode.FMOD : Opcode.IMOD);
                        break;
                    case Ast.BinaryOperator.Or:
                        code.Emit(Opcode.IOR);
                        break;
                    case Ast.BinaryOperator.And:
                        code.Emit(Opcode.IAND);
                        break;
                    case Ast.BinaryOperator.Xor:
                        code.Emit(Opcode.IXOR);
                        break;
                    case Ast.BinaryOperator.Equal:
                        code.Emit(isFloat ? Opcode.FEQ : Opcode.IEQ);
                        break;
                    case Ast.BinaryOperator.NotEqual:
                        code.Emit(isFloat ? Opcode.FNE : Opcode.INE);
                        break;
                    case Ast.BinaryOperator.Greater:
                        code.Emit(isFloat ? Opcode.FGT : Opcode.IGT);
                        break;
                    case Ast.BinaryOperator.GreaterOrEqual:
                        code.Emit(isFloat ? Opcode.FGE : Opcode.IGE);
                        break;
                    case Ast.BinaryOperator.Less:
                        code.Emit(isFloat ? Opcode.FLT : Opcode.ILT);
                        break;
                    case Ast.BinaryOperator.LessOrEqual:
                        code.Emit(isFloat ? Opcode.FLE : Opcode.ILE);
                        break;
                    default: throw new NotImplementedException();
                }
            }
            else if (operandsType is BasicType { TypeCode: BasicTypeCode.Bool })
            {
                // TODO: implement short-circuiting for AND/OR operators
                Left.EmitLoad(code);
                Right.EmitLoad(code);
                switch (Op)
                {
                    case Ast.BinaryOperator.LogicalAnd:
                        code.Emit(Opcode.IAND);
                        break;
                    case Ast.BinaryOperator.LogicalOr:
                        code.Emit(Opcode.IOR);
                        break;
                    case Ast.BinaryOperator.Equal:
                        code.Emit(Opcode.IEQ);
                        break;
                    case Ast.BinaryOperator.NotEqual:
                        code.Emit(Opcode.INE);
                        break;
                    default: throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitAddr(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundVariableExpression : BoundExpression
    {
        public override bool IsConstant => Var.IsConstant;
        public override bool IsAddressable => true;
        public override bool IsInvalid => false;
        public VariableSymbol Var { get; }

        public BoundVariableExpression(VariableSymbol variable)
        {
            Var = variable;
            Type = Var.Type;
        }

        public override void EmitLoad(ByteCodeBuilder code)
        {
            if (Var.IsLocal)
            {
                if (Type is RefType refType)
                {
                    code.EmitAddrLoadN(refType.ElementType.SizeOf, () => code.EmitLocalLoad(Var));
                }
                else
                {
                    code.EmitLocalLoad(Var);
                }
            }
            else if (Var.IsStatic)
            {
                if (Type is RefType)
                {
                    throw new NotSupportedException("Static variables cannot be references");
                }
                else
                {
                    code.EmitStaticLoad(Var);
                }
            }
            else if (Var.IsConstant)
            {
                Debug.Assert(Var.Initializer != null);
                Var.Initializer.EmitLoad(code);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override void EmitStore(ByteCodeBuilder code)
        {
            if (Var.IsLocal)
            {
                if (Type is RefType refType)
                {
                    code.EmitAddrStoreN(refType.ElementType.SizeOf, () => code.EmitLocalLoad(Var));
                }
                else
                {
                    code.EmitLocalStore(Var);
                }
            }
            else if (Var.IsStatic)
            {
                if (Type is RefType)
                {
                    throw new NotSupportedException("Static variables cannot be references");
                }
                else
                {
                    code.EmitStaticStore(Var);
                }
            }
            else if (Var.IsConstant)
            {
                throw new NotSupportedException("Constants cannot be written to");
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override void EmitAddr(ByteCodeBuilder code)
        {
            if (Var.IsLocal)
            {
                if (Type is RefType)
                {
                    // for references load the contained address instead of its own address
                    code.EmitLocalLoad(Var);
                }
                else
                {
                    code.EmitLocalAddr(Var);
                }
            }
            else if (Var.IsStatic)
            {
                if (Type is RefType)
                {
                    throw new NotSupportedException("Static variables cannot be references");
                }
                else
                {
                    code.EmitStaticAddr(Var);
                }
            }
            else if (Var.IsConstant)
            {
                throw new NotSupportedException("Cannot take address of constants");
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override void EmitCall(ByteCodeBuilder code)
        {
            if (Type is not FunctionType)
            {
                throw new InvalidOperationException("Only function types can be called");
            }

            EmitLoad(code);
            code.EmitIndirectCall();
        }
    }

    public sealed class BoundFunctionExpression : BoundExpression
    {
        public override bool IsConstant => true;
        public override bool IsAddressable => false;
        public override bool IsInvalid => false;
        public FunctionSymbol Function { get; }

        public BoundFunctionExpression(FunctionSymbol function)
        {
            Function = function;
            Type = Function.Type;
        }

        public override void EmitLoad(ByteCodeBuilder code) => code.EmitFuncAddr(Function);
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitAddr(ByteCodeBuilder code) => throw new NotSupportedException();

        public override void EmitCall(ByteCodeBuilder code)
        {
            if (Function.IsNative)
            {
                code.EmitNative(Function);
            }
            else
            {
                code.EmitCall(Function);
            }
        }
    }

    public sealed class BoundInvocationExpression : BoundExpression
    {
        public override bool IsConstant => false;
        public override bool IsAddressable => false;
        public override bool IsInvalid => Callee.IsInvalid || Arguments.Any(a => a.IsInvalid);
        public BoundExpression Callee { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }

        public BoundInvocationExpression(BoundExpression callee, IEnumerable<BoundExpression> arguments)
        {
            Callee = callee;
            Arguments = arguments.ToImmutableArray();

            if (!(callee is BoundInvalidExpression))
            {
                if (callee.Type is not FunctionType funcTy || funcTy.ReturnType == null)
                {
                    throw new ArgumentException("Callee type is not a function", nameof(callee));
                }

                Type = funcTy.ReturnType!;
            }
        }


        public override void EmitLoad(ByteCodeBuilder code)
        {
            var functionType = (Callee.Type as FunctionType)!;
            foreach (var (arg, (paramType, _)) in Arguments.Zip(functionType.Parameters))
            {
                if (paramType is RefType)
                {
                    arg.EmitAddr(code);
                }
                else
                {
                    arg.EmitLoad(code);
                }
            }
            Callee.EmitCall(code);
        }

        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitAddr(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundMemberAccessExpression : BoundExpression
    {
        public override bool IsConstant => false;
        public override bool IsAddressable => true;
        public override bool IsInvalid => Expression.IsInvalid;
        public BoundExpression Expression { get; }
        public string Member { get; }
        public int MemberOffset { get; }
        public bool IsArrayLength { get; } = false;
        public int ArrayLength { get; } = 0;

        public BoundMemberAccessExpression(BoundExpression expression, string member)
        {
            Expression = expression;
            Member = member;

            if (expression is not BoundInvalidExpression)
            {
                var ty = Expression.Type!.UnderlyingType;
                Debug.Assert(ty.HasField(Member));

                if (ty is ArrayType arrTy)
                {
                    Type = new BasicType(BasicTypeCode.Int);
                    IsArrayLength = true;
                    ArrayLength = arrTy.Length;
                }
                else
                {
                    Debug.Assert(ty is StructType);

                    var structTy = (ty as StructType)!;
                    MemberOffset = structTy.OffsetOfField(Member);
                    Type = structTy.TypeOfField(Member);
                }
            }
        }

        public override void EmitLoad(ByteCodeBuilder code)
        {
            if (IsArrayLength)
            {
                code.EmitPushInt(ArrayLength);
            }
            else
            {
                code.EmitOffsetLoadN(MemberOffset, Type!.SizeOf, () => Expression.EmitAddr(code));
            }
        }

        public override void EmitStore(ByteCodeBuilder code)
        {
            if (IsArrayLength)
            {
                throw new InvalidOperationException("Cannot modify array 'length' field");
            }

            code.EmitOffsetStoreN(MemberOffset, Type!.SizeOf, () => Expression.EmitAddr(code));
        }

        public override void EmitAddr(ByteCodeBuilder code)
        {
            if (IsArrayLength)
            {
                throw new InvalidOperationException("Cannot take address of array 'length' field");
            }

            Expression.EmitAddr(code);
            code.EmitOffsetAddr(MemberOffset);
        }

        public override void EmitCall(ByteCodeBuilder code)
        {
            if (Type is not FunctionType)
            {
                throw new InvalidOperationException("Only function types can be called");
            }

            EmitLoad(code);
            code.EmitIndirectCall();
        }
    }

    public sealed class BoundArrayAccessExpression : BoundExpression
    {
        public override bool IsConstant => false;
        public override bool IsAddressable => true;
        public override bool IsInvalid => Expression.IsInvalid || IndexExpression.IsInvalid;
        public BoundExpression Expression { get; }
        public BoundExpression IndexExpression { get; }

        public BoundArrayAccessExpression(BoundExpression expression, BoundExpression indexExpression)
        {
            Expression = expression;
            IndexExpression = indexExpression;

            if (expression is not BoundInvalidExpression)
            {
                Debug.Assert(expression.Type?.UnderlyingType is ArrayType);
                Debug.Assert(indexExpression.Type?.UnderlyingType is BasicType { TypeCode: BasicTypeCode.Int });

                var arrType = (Expression.Type!.UnderlyingType as ArrayType)!;
                Type = arrType.ItemType;
            }
        }

        public override void EmitLoad(ByteCodeBuilder code)
        {
            var itemSize = Type!.SizeOf;
            if (itemSize == 1)
            {
                IndexExpression.EmitLoad(code);
                Expression.EmitAddr(code);
                code.EmitArrayLoad(itemSize);
            }
            else
            {
                code.EmitAddrLoadN(itemSize, () => EmitAddr(code));
            }
        }

        public override void EmitStore(ByteCodeBuilder code)
        {
            var itemSize = Type!.SizeOf;
            if (itemSize == 1)
            {
                IndexExpression.EmitLoad(code);
                Expression.EmitAddr(code);
                code.EmitArrayStore(itemSize);
            }
            else
            {
                code.EmitAddrStoreN(itemSize, () => EmitAddr(code));
            }
        }

        public override void EmitAddr(ByteCodeBuilder code)
        {
            IndexExpression.EmitLoad(code);
            Expression.EmitAddr(code);
            code.EmitArrayAddr(Type!.SizeOf);
        }

        public override void EmitCall(ByteCodeBuilder code)
        {
            if (Type is not FunctionType)
            {
                throw new InvalidOperationException("Only function types can be called");
            }

            EmitLoad(code);
            code.EmitIndirectCall();
        }
    }

    public sealed class BoundAggregateExpression : BoundExpression
    {
        public override bool IsConstant => Expressions.All(expr => expr.IsConstant);
        public override bool IsAddressable => false;
        public override bool IsInvalid => Expressions.Any(expr => expr.IsInvalid);
        public ImmutableArray<BoundExpression> Expressions { get; }

        public BoundAggregateExpression(IEnumerable<BoundExpression> expressions)
        {
            Expressions = expressions.ToImmutableArray();
            Type = StructType.NewAggregate(Expressions.Select(e => e.Type!));
        }

        public override void EmitLoad(ByteCodeBuilder code)
        {
            foreach (var expr in Expressions)
            {
                expr.EmitLoad(code);
            }
        }

        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitAddr(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundFloatLiteralExpression : BoundExpression
    {
        private static readonly Type FloatType = new BasicType(BasicTypeCode.Float);

        public override bool IsConstant => true;
        public override bool IsAddressable => false;
        public override bool IsInvalid => false;
        public float Value { get; }

        public BoundFloatLiteralExpression(float value)
        {
            Value = value;
            Type = FloatType;
        }

        public override void EmitLoad(ByteCodeBuilder code) => code.EmitPushFloat(Value);
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitAddr(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundIntLiteralExpression : BoundExpression
    {
        private static readonly Type IntType = new BasicType(BasicTypeCode.Int);

        public override bool IsConstant => true;
        public override bool IsAddressable => false;
        public override bool IsInvalid => false;
        public int Value { get; }

        public BoundIntLiteralExpression(int value)
        {
            Value = value;
            Type = IntType;
        }

        public override void EmitLoad(ByteCodeBuilder code) => code.EmitPushInt(Value);
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitAddr(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundStringLiteralExpression : BoundExpression
    {
        private static readonly Type StringType = new BasicType(BasicTypeCode.String);

        public override bool IsConstant => false;
        public override bool IsAddressable => false;
        public override bool IsInvalid => false;
        public string Value { get; }

        public BoundStringLiteralExpression(string value)
        {
            Value = value;
            Type = StringType;
        }

        public override void EmitLoad(ByteCodeBuilder code) => code.EmitPushString(Value);
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitAddr(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundBoolLiteralExpression : BoundExpression
    {
        private static readonly Type BoolType = new BasicType(BasicTypeCode.Bool);

        public override bool IsConstant => true;
        public override bool IsAddressable => false;
        public override bool IsInvalid => false;
        public bool Value { get; }

        public BoundBoolLiteralExpression(bool value)
        {
            Value = value;
            Type = BoolType;
        }

        public override void EmitLoad(ByteCodeBuilder code) => code.EmitPushInt(Value ? 1 : 0);
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitAddr(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }
}

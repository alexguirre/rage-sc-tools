#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics.Symbols;

    using Opcode = ScTools.ScriptAssembly.Opcode;
    using Type = ScTools.ScriptLang.Semantics.Type;

    public abstract class BoundExpression : BoundNode
    {
        public Type Type { get; }


        public BoundExpression(Type type) => Type = type;

        public abstract void EmitLoad(ByteCodeBuilder code);
        public abstract void EmitStore(ByteCodeBuilder code);
        public abstract void EmitCall(ByteCodeBuilder code);
    }

    public sealed class BoundBinaryExpression : BoundExpression
    {
        public BoundExpression Left { get; }
        public BoundExpression Right { get; }
        public Ast.BinaryOperator Op { get; }

        public BoundBinaryExpression(BoundExpression left, BoundExpression right, Ast.BinaryOperator op)
            : base(left.Type)
        {
            Debug.Assert(left.Type == right.Type);
            Left = left;
            Right = right;
            Op = op;
        }

        public override void EmitLoad(ByteCodeBuilder code)
        {
            Left.EmitLoad(code);
            Right.EmitLoad(code);
            var isFloat = Type is BasicType { TypeCode: BasicTypeCode.Float };
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
            }
        }

        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundVariableExpression : BoundExpression
    {
        public VariableSymbol Var { get; }

        public BoundVariableExpression(VariableSymbol variable)
            : base(variable.Type)
        {
            Var = variable;
        }

        public override void EmitLoad(ByteCodeBuilder code)
        {
            Debug.Assert(Type.SizeOf == 1, "EmitLoad for variable with size of type > 1 not implemented");

            if (Var.IsLocal)
            {
                code.EmitLocalLoad(Var.Location);
            }
            else if (Var.IsStatic)
            {
                code.EmitStaticLoad(Var.Location);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override void EmitStore(ByteCodeBuilder code)
        {
            Debug.Assert(Type.SizeOf == 1, "EmitStore for variable with size of type > 1 not implemented");

            if (Var.IsLocal)
            {
                code.EmitLocalStore(Var.Location);
            }
            else if (Var.IsStatic)
            {
                code.EmitStaticStore(Var.Location);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override void EmitCall(ByteCodeBuilder code)
        {
            Debug.Assert(Type is FunctionType, "Only function types can be called");

            throw new NotImplementedException();
        }
    }

    public sealed class BoundFunctionExpression : BoundExpression
    {
        public FunctionSymbol Function { get; }

        public BoundFunctionExpression(FunctionSymbol function)
            : base(function.Type)
        {
            Function = function;
        }

        public override void EmitLoad(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();

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
        public BoundExpression Callee { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }

        public BoundInvocationExpression(BoundExpression callee, IEnumerable<BoundExpression> arguments)
            : base((callee.Type as FunctionType)?.ReturnType!)
        {
            Debug.Assert(callee.Type is FunctionType f && f.ReturnType != null);

            Callee = callee;
            Arguments = arguments.ToImmutableArray();
        }


        public override void EmitLoad(ByteCodeBuilder code)
        {
            foreach (var arg in Arguments)
            {
                arg.EmitLoad(code);
            }
            Callee.EmitCall(code);
        }

        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundFloatLiteralExpression : BoundExpression
    {
        private static readonly Type FloatType = new BasicType(BasicTypeCode.Float);

        public float Value { get; }

        public BoundFloatLiteralExpression(float value)
            : base(FloatType)
        {
            Value = value;
        }

        public override void EmitLoad(ByteCodeBuilder code) => code.EmitPushFloat(Value);
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundIntLiteralExpression : BoundExpression
    {
        private static readonly Type IntType = new BasicType(BasicTypeCode.Int);

        public int Value { get; }

        public BoundIntLiteralExpression(int value)
            : base(IntType)
        {
            Value = value;
        }

        public override void EmitLoad(ByteCodeBuilder code) => code.EmitPushInt(Value);
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundStringLiteralExpression : BoundExpression
    {
        private static readonly Type StringType = new BasicType(BasicTypeCode.String);

        public string Value { get; }

        public BoundStringLiteralExpression(string value)
            : base(StringType)
        {
            Value = value;
        }

        public override void EmitLoad(ByteCodeBuilder code) => code.EmitPushString(Value);
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }

    public sealed class BoundBoolLiteralExpression : BoundExpression
    {
        private static readonly Type BoolType = new BasicType(BasicTypeCode.Bool);

        public bool Value { get; }

        public BoundBoolLiteralExpression(bool value)
            : base(BoolType)
        {
            Value = value;
        }

        public override void EmitLoad(ByteCodeBuilder code) => code.EmitPushInt(Value ? 1 : 0);
        public override void EmitStore(ByteCodeBuilder code) => throw new NotSupportedException();
        public override void EmitCall(ByteCodeBuilder code) => throw new NotSupportedException();
    }
}

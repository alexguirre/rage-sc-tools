#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Runtime.InteropServices.ComTypes;

    using ScTools.ScriptLang.Semantics.Symbols;

    public enum BoundExpressionFlags
    {
        None = 0,
        /// <summary>
        /// Can be used as the RHS of an assignment statement.
        /// </summary>
        RValue = 1 << 0,
        /// <summary>
        /// Can be used as the LHS of an assignment statement.
        /// </summary>
        Assignable = 1 << 1,
        /// <summary>
        /// Can be used as the callee of an invocation.
        /// </summary>
        Callable = 1 << 2,
    }

    public abstract class BoundExpression : BoundNode
    {
        public Type Type { get; }
        public abstract BoundExpressionFlags Flags { get; }
        public bool IsRValue => Flags.HasFlag(BoundExpressionFlags.RValue);
        public bool IsAssignable => Flags.HasFlag(BoundExpressionFlags.Assignable);


        public BoundExpression(Type type) => Type = type;
    }

    public sealed class BoundBinaryExpression : BoundExpression
    {
        public override BoundExpressionFlags Flags => BoundExpressionFlags.RValue;

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
    }

    public sealed class BoundVariableExpression : BoundExpression
    {
        public override BoundExpressionFlags Flags { get; }

        public VariableSymbol Var { get; }

        public BoundVariableExpression(VariableSymbol variable)
            : base(variable.Type)
        {
            Var = variable;
            Flags = BoundExpressionFlags.RValue | BoundExpressionFlags.Assignable;
            if (Type is FunctionType)
            {
                Flags |= BoundExpressionFlags.Callable;
            }
        }
    }

    public sealed class BoundFunctionExpression : BoundExpression
    {
        public override BoundExpressionFlags Flags { get; }

        public FunctionSymbol Function { get; }

        public BoundFunctionExpression(FunctionSymbol function)
            : base(function.Type)
        {
            Function = function;
            Flags = BoundExpressionFlags.RValue | BoundExpressionFlags.Callable;
        }
    }

    public sealed class BoundInvocationExpression : BoundExpression
    {
        public override BoundExpressionFlags Flags { get; }

        public BoundExpression Callee { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }

        public BoundInvocationExpression(BoundExpression callee, IEnumerable<BoundExpression> arguments)
            : base((callee.Type as FunctionType)?.ReturnType!)
        {
            Debug.Assert(callee.Type is FunctionType f && f.ReturnType != null);

            Callee = callee;
            Arguments = arguments.ToImmutableArray();
            
            Flags = BoundExpressionFlags.RValue;
            if (Type is FunctionType)
            {
                Flags |= BoundExpressionFlags.Callable;
            }
        }
    }

    public sealed class BoundFloatLiteralExpression : BoundExpression
    {
        private static readonly Type FloatType = new BasicType(BasicTypeCode.Float);

        public override BoundExpressionFlags Flags => BoundExpressionFlags.RValue;
        public float Value { get; }

        public BoundFloatLiteralExpression(float value)
            : base(FloatType)
        {
            Value = value;
        }
    }

    public sealed class BoundIntLiteralExpression : BoundExpression
    {
        private static readonly Type IntType = new BasicType(BasicTypeCode.Int);

        public override BoundExpressionFlags Flags => BoundExpressionFlags.RValue;
        public int Value { get; }

        public BoundIntLiteralExpression(int value)
            : base(IntType)
        {
            Value = value;
        }
    }
}

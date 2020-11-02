#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;

    using ScTools.ScriptLang.CodeGen;

    public abstract class BoundStatement : BoundNode
    {
        public abstract void Emit(ByteCodeBuilder code, BoundFunction parent);
    }

    public sealed class BoundAssignmentStatement : BoundStatement
    {
        public BoundExpression Left { get; }
        public BoundExpression Right { get; }

        public BoundAssignmentStatement(BoundExpression left, BoundExpression right)
            => (Left, Right) = (left, right);

        public override void Emit(ByteCodeBuilder code, BoundFunction parent)
        {
            Right.EmitLoad(code);
            Left.EmitStore(code);
        }
    }

    public sealed class BoundInvocationStatement : BoundStatement
    {
        public BoundExpression Callee { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }

        public BoundInvocationStatement(BoundExpression callee, IEnumerable<BoundExpression> arguments)
        {
            Debug.Assert(callee.Type is FunctionType f && f.ReturnType != null);

            Callee = callee;
            Arguments = arguments.ToImmutableArray();
        }

        public override void Emit(ByteCodeBuilder code, BoundFunction parent)
        {
            foreach (var arg in Arguments)
            {
                arg.EmitLoad(code);
            }
            Callee.EmitCall(code);

            // since this is a statement, drop the returned values if any
            var returnType = (Callee.Type as FunctionType)!.ReturnType;
            if (returnType != null)
            {
                for (int i = 0; i < returnType.SizeOf; i++)
                {
                    code.Emit(ScriptAssembly.Opcode.DROP);
                }
            }
        }
    }

    public sealed class BoundReturnStatement : BoundStatement
    {
        public BoundExpression? Expression { get; }

        public BoundReturnStatement(BoundExpression? expression)
        {
            Expression = expression;
        }

        public override void Emit(ByteCodeBuilder code, BoundFunction parent)
        {
            Expression?.EmitLoad(code);
            code.EmitEpilogue(parent.Function);
        }
    }
}

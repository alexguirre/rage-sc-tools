#nullable enable
namespace ScTools.ScriptLang.Semantics.Binding
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;

    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics.Symbols;

    public abstract class BoundStatement : BoundNode
    {
        public abstract void Emit(ByteCodeBuilder code, BoundFunction parent);
    }

    public sealed class BoundVariableDeclarationStatement : BoundStatement
    {
        public VariableSymbol Var { get; }
        public BoundExpression? Initializer { get; }

        public BoundVariableDeclarationStatement(VariableSymbol var, BoundExpression? initializer)
            => (Var, Initializer) = (var, initializer);

        public override void Emit(ByteCodeBuilder code, BoundFunction parent)
        {
            Debug.Assert(Var.IsLocal, "Emit for static variables not implemented");
            Debug.Assert(Var.Type.SizeOf == 1, "Emit for variable with size of type > 1 not implemented");

            if (Initializer != null)
            {
                Initializer.EmitLoad(code);
                code.EmitLocalStore(Var.Location);
            }
        }
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

    public sealed class BoundIfStatement : BoundStatement
    {
        public BoundExpression Condition { get; }
        public IList<BoundStatement> Then { get; } = new List<BoundStatement>();
        public IList<BoundStatement> Else { get; } = new List<BoundStatement>();

        private readonly string id = Guid.NewGuid().ToString(); // unique ID used for labels

        public BoundIfStatement(BoundExpression condition)
            => Condition = condition;

        public override void Emit(ByteCodeBuilder code, BoundFunction parent)
        {
            var elseLabel = id;
            
            // if
            Condition.EmitLoad(code);
            code.EmitJumpIfZero(elseLabel);
            
            // then
            foreach (var stmt in Then)
            {
                stmt.Emit(code, parent);
            }

            // else
            code.AddLabel(elseLabel);
            foreach (var stmt in Else)
            {
                stmt.Emit(code, parent);
            }
        }
    }

    public sealed class BoundWhileStatement : BoundStatement
    {
        public BoundExpression Condition { get; }
        public IList<BoundStatement> Block { get; } = new List<BoundStatement>();

        private readonly string id = Guid.NewGuid().ToString(); // unique ID used for labels

        public BoundWhileStatement(BoundExpression condition)
            => Condition = condition;

        public override void Emit(ByteCodeBuilder code, BoundFunction parent)
        {
            var beginLabel = id + "-begin";
            var exitLabel = id + "-exit";

            code.AddLabel(beginLabel);
            Condition.EmitLoad(code);
            code.EmitJumpIfZero(exitLabel);

            foreach (var stmt in Block)
            {
                stmt.Emit(code, parent);
            }
            code.EmitJump(beginLabel);

            code.AddLabel(exitLabel);
        }
    }

    public sealed class BoundInvocationStatement : BoundStatement
    {
        public BoundExpression Callee { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }

        public BoundInvocationStatement(BoundExpression callee, IEnumerable<BoundExpression> arguments)
        {
            Debug.Assert(callee.Type is FunctionType f);

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

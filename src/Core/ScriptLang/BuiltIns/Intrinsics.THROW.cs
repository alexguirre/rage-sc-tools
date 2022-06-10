namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Collections.Immutable;

public static partial class Intrinsics
{
    /// <summary>
    /// Throws an exception with the specified error code.
    /// Exceptions can be handled with the `CATCH` intrinsic.
    /// <br/>
    /// Signature: THROW (INT) -> VOID
    /// </summary>
    private sealed class IntrinsicTHROW : BaseFunctionLikeIntrinsic
    {
        public new const string Name = "THROW";
        public new static readonly FunctionType Type = new(
            Return: VoidType.Instance,
            Parameters: ImmutableArray.Create(
                new ParameterInfo(IntType.Instance, IsReference: false)));

        public IntrinsicTHROW() : base(Name, Type, canBeConstant: false)
        {
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            ThrowCannotBeConstantEvaluated(this);
            return null;
        }

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
        {
            c.EmitValue(node.Arguments[0]);
            c.EmitThrow();
        }
    }
}

namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Collections.Immutable;

public static partial class Intrinsics
{
    /// <summary>
    /// Handles exceptions thrown with the `THROW` intrinsic.
    /// When invoked, sets up the exception handler and returns <c>-1</c> to indicate that no exception occurred.
    /// When an exception occurs, the execution is transfered to the last `CATCH` and the return value is set to the exception error code.
    /// <br/>
    /// Signature: CATCH () -> INT
    /// </summary>
    private sealed class IntrinsicCATCH : BaseFunctionLikeIntrinsic
    {
        public new const string Name = "CATCH";
        public new static readonly FunctionType Type = new(
            Return: IntType.Instance,
            Parameters: ImmutableArray<ParameterInfo>.Empty);

        public IntrinsicCATCH() : base(Name, Type, canBeConstant: false)
        {
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            ThrowCannotBeConstantEvaluated(this);
            return null;
        }

        public override void CodeGen(InvocationExpression node, CodeEmitter c)
        {
            c.EmitCatch();
        }
    }
}

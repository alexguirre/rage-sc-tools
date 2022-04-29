namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Collections.Immutable;

public static partial class Intrinsics
{
    /// <summary>
    /// Creates a VECTOR with the same value for all components.
    /// <br/>
    /// Signature: F2V (FLOAT) -> VECTOR
    /// </summary>
    private sealed class IntrinsicF2V : BaseFunctionLikeIntrinsic
    {
        public new const string Name = "F2V";
        public new static readonly FunctionType Type = new(
            Return: VectorType.Instance,
            Parameters: ImmutableArray.Create(
                new ParameterInfo(FloatType.Instance, IsReference: false)));

        public IntrinsicF2V() : base(Name, Type)
        {
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            var value = ConstantExpressionEvaluator.Eval(node.Arguments[0], semantics);
            return ConstantValue.Vector(value.FloatValue, value.FloatValue, value.FloatValue);
        }
    }
}

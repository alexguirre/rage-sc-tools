namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Collections.Immutable;

public static partial class Intrinsics
{
    /// <summary>
    /// Checks whether the bit at the specified position is set. Replacement for the native `IS_BIT_SET` since GTAV b2612.
    /// <br/>
    /// Signature: IS_BIT_SET(INT value, INT position) -> BOOL
    /// </summary>
    private sealed class IntrinsicIS_BIT_SET : BaseFunctionLikeIntrinsic
    {
        public new const string Name = "IS_BIT_SET";
        public new static readonly FunctionType Type = new(
            Return: BoolType.Instance,
            Parameters: ImmutableArray.Create<ParameterInfo>(
                new(IntType.Instance, IsReference: false),
                new(IntType.Instance, IsReference: false)));

        public IntrinsicIS_BIT_SET() : base(Name, Type)
        {
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            var value = ConstantExpressionEvaluator.Eval(node.Arguments[0], semantics).IntValue;
            var position = ConstantExpressionEvaluator.Eval(node.Arguments[1], semantics).IntValue;
            return ConstantValue.Bool((value & (1 << position)) != 0);
        }

        public override void CodeGen(InvocationExpression node, ICodeEmitter codeEmitter)
            => throw new System.NotImplementedException();
    }
}

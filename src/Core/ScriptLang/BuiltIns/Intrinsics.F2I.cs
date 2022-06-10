namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Collections.Immutable;

public static partial class Intrinsics
{
    /// <summary>
    /// Casts a FLOAT to INT.
    /// <br/>
    /// Signature: F2I (FLOAT) -> INT
    /// </summary>
    private sealed class IntrinsicF2I : BaseFunctionLikeIntrinsic
    {
        public new const string Name = "F2I";
        public new static readonly FunctionType Type = new(
            Return: IntType.Instance,
            Parameters: ImmutableArray.Create(
                new ParameterInfo(FloatType.Instance, IsReference: false)));

        public IntrinsicF2I() : base(Name, Type)
        {
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            var value = ConstantExpressionEvaluator.Eval(node.Arguments[0], semantics);
            return ConstantValue.Int((int)value.FloatValue);
        }

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
        {
            c.EmitValue(node.Arguments[0]);
            c.EmitCastFloatToInt();
        }
    }
}

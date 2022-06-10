namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Collections.Immutable;

public static partial class Intrinsics
{
    /// <summary>
    /// Casts an INT to FLOAT.
    /// <br/>
    /// Signature: I2F (INT) -> FLOAT
    /// </summary>
    private sealed class IntrinsicI2F : BaseFunctionLikeIntrinsic
    {
        public new const string Name = "I2F";
        public new static readonly FunctionType Type = new(
            Return: FloatType.Instance,
            Parameters: ImmutableArray.Create(
                new ParameterInfo(IntType.Instance, IsReference: false)));

        public IntrinsicI2F() : base(Name, Type)
        {
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            var value = ConstantExpressionEvaluator.Eval(node.Arguments[0], semantics);
            return ConstantValue.Float(value.IntValue);
        }

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
        {
            c.EmitValue(node.Arguments[0]);
            c.EmitCastIntToFloat();
        }
    }
}

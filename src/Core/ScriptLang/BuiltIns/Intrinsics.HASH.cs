namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Collections.Immutable;

public static partial class Intrinsics
{
    /// <summary>
    /// Calculate the hash of the string argument. If the argument is a string literal, it is calculated at compile-time.
    /// <br/>
    /// Signature: HASH (STRING) -> INT
    /// </summary>
    private sealed class IntrinsicHASH : BaseFunctionLikeIntrinsic
    {
        public new const string Name = "HASH";
        public new static readonly FunctionType Type = new(
            Return: IntType.Instance,
            Parameters: ImmutableArray.Create(
                new ParameterInfo(StringType.Instance, IsReference: false)));

        public IntrinsicHASH() : base(Name, Type)
        {
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            var value = ConstantExpressionEvaluator.Eval(node.Arguments[0], semantics);
            return ConstantValue.Int(unchecked((int)(value.StringValue?.ToLowercaseHash() ?? 0)));
        }

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
        {
            if (node.Arguments[0] is StringLiteralExpression { Value: var strLiteral })
            {
                // calculate the hash at compile-time if the argument is a string literal
                c.EmitPushInt(unchecked((int)strLiteral.ToLowercaseHash()));
            }
            else
            {
                c.EmitValue(node.Arguments[0]);
                c.EmitStringHash();
            }
        }
    }
}

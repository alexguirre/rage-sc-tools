namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Diagnostics;
using System.Linq;

public static partial class Intrinsics
{
    /// <summary>
    /// Gets the length of the array.
    /// <br/>
    /// Signature: COUNT_OF (ARRAY) -> INT
    /// </summary>
    private sealed class IntrinsicCOUNT_OF : BaseIntrinsic
    {
        public new const string Name = "COUNT_OF";

        public IntrinsicCOUNT_OF() : base(Name)
        {
        }

        public override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
        {
            UsagePrecondition(node);

            var argTypes = node.Arguments.Select(a => a.Accept(exprTypeChecker, semantics)).ToArray();

            // type-check arguments
            var args = node.Arguments;
            ExpressionTypeChecker.CheckArgumentCount(parameterCount: 1, node, semantics);
            
            // check that the argument is an array
            bool isIncomplete = false;
            if (argTypes.Length > 0 && !argTypes[0].IsError)
            {
                if (argTypes[0] is ArrayType arrTy)
                {
                    isIncomplete = arrTy.IsIncomplete;
                }
                else
                {
                    ExpressionTypeChecker.ArgNotAnArrayError(semantics, 0, args[0], argTypes[0]);
                }
            }

            return new(IntType.Instance, ValueKind.RValue | (isIncomplete ? 0 : ValueKind.Constant), ArgumentKind.None);
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            var arrType = (ArrayType)node.Arguments[0].Type!;
            if (arrType.IsIncomplete)
            {
                throw new NotSupportedException($"Intrinsic '{Name}' with incomplete array cannot be constant-evaluated.");
            }

            return ConstantValue.Int(arrType.Length);
        }

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
        {
            var arrType = (ArrayType)node.Arguments[0].Type!;
            if (arrType.IsIncomplete)
            {
                c.EmitPushArrayLength(node.Arguments[0]);
            }
            else
            {
                c.EmitPushInt(arrType.Length);
            }
        }
    }
}

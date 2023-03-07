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
    /// Casts an native type value to the corresponding INT value.
    /// <br/>
    /// Signature: NATIVE_TO_INT (NATIVE) -> INT
    /// </summary>
    private sealed class IntrinsicNATIVE_TO_INT : BaseIntrinsic
    {
        public new const string Name = "NATIVE_TO_INT";

        public IntrinsicNATIVE_TO_INT() : base(Name)
        {
        }

        public override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
        {
            UsagePrecondition(node);

            var argTypes = node.Arguments.Select(a => a.Accept(exprTypeChecker, semantics)).ToArray();

            // type-check arguments
            var args = node.Arguments;
            ExpressionTypeChecker.CheckArgumentCount(parameterCount: 1, node, semantics);

            // check that the argument is a NATIVE type value
            if (argTypes.Length > 0  && !argTypes[0].IsError && argTypes[0] is not NativeType)
            {
                ExpressionTypeChecker.ArgNotANativeTypeValueError(semantics, 0, args[0], argTypes[0]);
            }

            return new(IntType.Instance, ValueKind.RValue, ArgumentKind.None);
        }

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
        {
            c.EmitValue(node.Arguments[0]);
        }
    }
}

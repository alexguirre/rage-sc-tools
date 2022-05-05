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
    /// Casts an ENUM value to the corresponding INT value.
    /// <br/>
    /// Signature: ENUM_TO_INT (ENUM) -> INT
    /// </summary>
    private sealed class IntrinsicENUM_TO_INT : BaseIntrinsic
    {
        public new const string Name = "ENUM_TO_INT";

        public IntrinsicENUM_TO_INT() : base(Name)
        {
        }

        public override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
        {
            Debug.Assert(node.Callee is NameExpression nameExpr && ReferenceEquals(nameExpr.Semantics.Declaration, this));

            // NOTE: invocation type-checking code copied from ExpressionTypeChecker
            var argTypes = node.Arguments.Select(a => a.Accept(exprTypeChecker, semantics)).ToArray();

            // type-check arguments
            const int ParameterCount = 1;
            var args = node.Arguments;
            if (ParameterCount != args.Length)
            {
                ExpressionTypeChecker.MismatchedArgumentCountError(semantics, ParameterCount, node);
            }

            // check that the argument is an ENUM value
            if (argTypes.Length > 0 && argTypes[0] is not EnumType)
            {
                ExpressionTypeChecker.ArgNotAnEnumError(semantics, 0, args[0], argTypes[0]);
            }

            return new(IntType.Instance, ValueKind.RValue | (args[0].ValueKind & ValueKind.Constant), ArgumentKind.None);
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            var v = ConstantExpressionEvaluator.Eval(node.Arguments[0], semantics);
            return ConstantValue.Int(v.IntValue);
        }

        public override void CodeGen(InvocationExpression node, CodeEmitter c)
        {
            c.EmitValue(node.Arguments[0]);
        }
    }
}

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
    /// Gets the size of a expression or type.
    /// <br/>
    /// Signature: SIZE_OF (expression|TYPENAME) -> INT
    /// </summary>
    private sealed class IntrinsicSIZE_OF : BaseIntrinsic
    {
        public new const string Name = "SIZE_OF";

        public IntrinsicSIZE_OF() : base(Name)
        {
        }

        public override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
        {
            UsagePrecondition(node);

            node.Arguments.ForEach(a => a.Accept(exprTypeChecker, semantics));

            // type-check arguments
            ExpressionTypeChecker.CheckArgumentCount(parameterCount: 1, node, semantics);

            return new(IntType.Instance, ValueKind.RValue | ValueKind.Constant, ArgumentKind.None);
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
            => ConstantValue.Int(GetTypeFromArgument(node).SizeOf);

        public override void CodeGen(InvocationExpression node, ICodeEmitter c)
            => c.EmitPushInt(GetTypeFromArgument(node).SizeOf);

        private static TypeInfo GetTypeFromArgument(InvocationExpression node)
        {
            TypeInfo type = node.Arguments[0].Type!;
            if (type is TypeNameType typeName)
            {
                type = typeName.TypeSymbol.DeclaredType;
            }
            return type;
        }
    }
}

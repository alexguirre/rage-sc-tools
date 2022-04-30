namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Expressions;
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

            return new(IntType.Instance, ValueKind.RValue | ValueKind.Constant);
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            TypeInfo type = node.Arguments[0].Type!;
            if (type is TypeNameType typeName)
            {
                type = typeName.TypeDeclaration.Semantics.DeclaredType!;
            }

            return ConstantValue.Int(type.SizeOf);
        }
    }
}

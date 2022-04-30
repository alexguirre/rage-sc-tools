namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Diagnostics;
using System.Linq;

public static partial class Intrinsics
{
    /// <summary>
    /// Casts an INT value to an ENUM type.
    /// <br/>
    /// Signature: INT_TO_ENUM (ENUMNAME, INT) -> ENUM
    /// </summary>
    private sealed class IntrinsicINT_TO_ENUM : BaseIntrinsic
    {
        public new const string Name = "INT_TO_ENUM";

        public IntrinsicINT_TO_ENUM() : base(Name)
        {
        }

        public override ExpressionSemantics InvocationTypeCheck(InvocationExpression node, SemanticsAnalyzer semantics, ExpressionTypeChecker exprTypeChecker)
        {
            Debug.Assert(node.Callee is NameExpression nameExpr && ReferenceEquals(nameExpr.Semantics.Declaration, this));

            // NOTE: invocation type-checking code copied from ExpressionTypeChecker
            var argTypes = node.Arguments.Select(a => a.Accept(exprTypeChecker, semantics)).ToArray();

            // type-check arguments
            const int ParameterCount = 2;
            var args = node.Arguments;
            if (ParameterCount != args.Length)
            {
                ExpressionTypeChecker.MismatchedArgumentCountError(semantics, ParameterCount, node);
            }

            TypeInfo returnType = ErrorType.Instance;
            if (argTypes.Length > 0)
            {
                if (argTypes[0] is TypeNameType { TypeDeclaration: EnumDeclaration enumDecl })
                {
                    returnType = enumDecl.Semantics.DeclaredType!;
                }
                else
                {
                    ExpressionTypeChecker.ArgNotAnEnumTypeError(semantics, 0, args[0]);
                }
            }

            if (argTypes.Length > 1 && !IntType.Instance.IsAssignableFrom(argTypes[1]))
            {
                ExpressionTypeChecker.ArgCannotPassTypeError(semantics, 1, args[1], argTypes[1], IntType.Instance);
            }

            return new(returnType, ValueKind.RValue | (args[0].ValueKind & args[1].ValueKind & ValueKind.Constant));
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            var v = ConstantExpressionEvaluator.Eval(node.Arguments[1], semantics);
            return ConstantValue.Int(v.IntValue);
        }
    }
}

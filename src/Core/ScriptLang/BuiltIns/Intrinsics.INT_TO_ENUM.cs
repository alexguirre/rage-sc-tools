namespace ScTools.ScriptLang.BuiltIns;

using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.CodeGen;
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
            var args = node.Arguments;
            ExpressionTypeChecker.CheckArgumentCount(parameterCount: 2, node, semantics);

            TypeInfo returnType = ErrorType.Instance;
            ValueKind constantFlag = ValueKind.Constant;
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

            if (argTypes.Length > 1)
            {
                ExpressionTypeChecker.TypeCheckArgumentAgainstParameter(1, args[1], new(IntType.Instance, IsReference: false), semantics);
                constantFlag &= args[1].ValueKind;
            }

            return new(returnType, ValueKind.RValue | constantFlag, ArgumentKind.None);
        }

        public override ConstantValue ConstantEval(InvocationExpression node, SemanticsAnalyzer semantics)
        {
            var v = ConstantExpressionEvaluator.Eval(node.Arguments[1], semantics);
            return ConstantValue.Int(v.IntValue);
        }

        public override void CodeGen(InvocationExpression node, CodeEmitter c)
        {
            c.EmitValue(node.Arguments[1]);
        }
    }
}

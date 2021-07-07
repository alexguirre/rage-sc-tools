namespace ScTools.ScriptLang.BuiltIns
{
    using System;
    using System.Diagnostics;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.SymbolTables;

    public static partial class Intrinsics
    {
        private sealed class EnumToIntIntrinsic : BaseValueDeclaration, IIntrinsicDeclaration
        {
            public EnumToIntIntrinsic() : base(SourceRange.Unknown, "ENUM_TO_INT", new EnumToIntIntrinsicType()) { }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();

            public int EvalInt(InvocationExpression expr, GlobalSymbolTable symbols)
            {
                return ExpressionEvaluator.EvalInt(expr.Arguments[0], symbols);
            }

            public float EvalFloat(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public bool EvalBool(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public string EvalString(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public (float X, float Y, float Z) EvalVector(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
        }

        private sealed class EnumToIntIntrinsicType : BaseType
        {
            public override int SizeOf => throw new NotImplementedException();

            public EnumToIntIntrinsicType() : base(SourceRange.Unknown) { }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();

            public override bool Equivalent(IType other) => other is CountOfIntrinsicType;

            public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs is ErrorType;

            public override (IType ReturnType, bool IsConstant) Invocation(IExpression[] args, SourceRange source, DiagnosticsReport diagnostics)
            {
                var returnType = new IntType(source);

                if (args.Length != 1)
                {
                    diagnostics.AddError($"Expected 1 argument, found {args.Length}", source);
                }

                if (args.Length < 1)
                {
                    return (returnType, false);
                }

                var arg = args[0];
                if (arg.Type is not (EnumType or ErrorType))
                {
                    diagnostics.AddError($"Argument 1: cannot pass '{arg.Type}' to ENUM_TO_INT parameter, expected enum value", arg.Source);
                }

                return (returnType, arg.IsConstant);
            }

            public override void CGInvocation(CodeGenerator cg, InvocationExpression expr)
            {
                cg.EmitValue(expr.Arguments[0]);
            }

            public override string ToString() => $"INTRINSIC INT ENUM_TO_INT(<enum>)";
        }
    }
}

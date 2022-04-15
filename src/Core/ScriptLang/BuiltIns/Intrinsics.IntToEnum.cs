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
        private sealed class IntToEnumIntrinsic : BaseValueDeclaration, IIntrinsicDeclaration
        {
            public IntToEnumIntrinsic() : base(SourceRange.Unknown, "INT_TO_ENUM", new IntToEnumIntrinsicType()) { }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();

            public int EvalInt(InvocationExpression expr, GlobalSymbolTable symbols)
            {
                return ExpressionEvaluator.EvalInt(expr.Arguments[1], symbols);
            }

            public float EvalFloat(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public bool EvalBool(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public string EvalString(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public (float X, float Y, float Z) EvalVector(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
        }

        private sealed class IntToEnumIntrinsicType : BaseType
        {
            public override int SizeOf => throw new NotImplementedException();

            public IntToEnumIntrinsicType() : base(SourceRange.Unknown) { }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();

            public override bool Equivalent(IType other) => other is CountOfIntrinsicType;

            public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs is ErrorType;

            public override (IType ReturnType, bool IsConstant) Invocation(IExpression[] args, SourceRange source, DiagnosticsReport diagnostics)
            {
                if (args.Length != 2)
                {
                    var err = new ErrorType(source, diagnostics, $"Expected 2 arguments, found {args.Length}");
                    if (args.Length < 1)
                    {
                        return (err, false);
                    }
                }

                IType returnType;
                var arg1 = args[0];
                if (arg1.Type is TypeNameType typeName)
                {
                    // FIXME
                    //if (typeName.TypeDeclaration is EnumDeclaration enumDecl)
                    //{
                    //    returnType = enumDecl.CreateType(source);
                    //}
                    //else
                    //{
                    //    returnType = new ErrorType(arg1.Location, diagnostics, $"Argument 1: cannot pass non-enum type '{typeName.TypeDeclaration.Name}' to INT_TO_ENUM first parameter");
                    //}
                }
                else if (arg1.Type is ErrorType)
                {
                    returnType = arg1.Type;
                }
                else
                {
                    returnType = new ErrorType(arg1.Location, diagnostics, $"Argument 1: cannot pass '{arg1.Type}' to INT_TO_ENUM first parameter, expected enum type name");
                }

                var isConstant = false;
                if (args.Length >= 2)
                {
                    var arg2 = args[1];
                    isConstant = arg2.IsConstant;
                    var param2Ty = new IntType(arg2.Location);
                    if (!param2Ty.CanAssign(arg2.Type!, arg2.IsLValue))
                    {
                        diagnostics.AddError($"Argument  2: cannot pass '{arg2.Type}' as second parameter '{TypePrinter.ToString(param2Ty, "value", false)}'", arg2.Location);
                    }
                }

                // FIXME
                return (/*returnType*/null, isConstant);
            }

            public override void CGInvocation(CodeGenerator cg, InvocationExpression expr)
            {
                cg.EmitValue(expr.Arguments[1]);
            }

            public override string ToString() => $"INTRINSIC <enum type> INT_TO_ENUM(<enum type>, INT value)";
        }
    }
}

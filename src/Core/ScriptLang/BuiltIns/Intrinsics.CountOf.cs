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
    using ScTools.ScriptLang.SymbolTables;

    public static partial class Intrinsics
    {
        private sealed class CountOfIntrinsic : BaseValueDeclaration, IIntrinsicDeclaration
        {
            public CountOfIntrinsic() : base(SourceRange.Unknown, "COUNT_OF", new CountOfIntrinsicType()) { }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();

            public int EvalInt(InvocationExpression expr, GlobalSymbolTable symbols)
            {
                if (expr.Arguments[0].Type is TypeNameType { TypeDeclaration: EnumDeclaration enumDecl })
                {
                    return enumDecl.Members.Count;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            public float EvalFloat(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public bool EvalBool(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public string EvalString(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
            public (float X, float Y, float Z) EvalVector(InvocationExpression expr, GlobalSymbolTable symbols) => throw new NotImplementedException();
        }

        private sealed class CountOfIntrinsicType : BaseType
        {
            public override int SizeOf => throw new NotImplementedException();

            public CountOfIntrinsicType() : base(SourceRange.Unknown) { }

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

                var isConstant = false;
                var arg = args[0];
                if (arg.Type is IArrayType)
                {
                    if (!arg.IsLValue)
                    {
                        Debug.Assert(false, "There is no way for an array to be non-lvalue");
                    }
                }
                else if (arg.Type is TypeNameType typeName)
                {
                    if (typeName.TypeDeclaration is not EnumDeclaration)
                    {
                        diagnostics.AddError($"Argument 1: cannot pass non-enum type '{typeName.TypeDeclaration.Name}' to COUNT_OF parameter", arg.Location);
                    }
                    else
                    {
                        isConstant = true;
                    }
                }
                else if (arg.Type is not ErrorType)
                {
                    diagnostics.AddError($"Argument 1: cannot pass '{arg.Type}' to COUNT_OF parameter, expected array reference or enum type name", arg.Location);
                }

                return (returnType, isConstant);
            }

            public override void CGInvocation(CodeGenerator cg, InvocationExpression expr)
            {
                var arg = expr.Arguments[0];
                if (arg.Type is IArrayType arrayTy)
                {
                    if (arrayTy is ArrayType constantSizeArrayTy)
                    {
                        // Size known at compile-time
                        cg.EmitPushConstInt(constantSizeArrayTy.Length);
                    }
                    else
                    {
                        // Size known at runtime. The size is store at offset 0 of the array
                        cg.EmitAddress(arg);
                        cg.Emit(Opcode.LOAD);
                    }
                }
                else if (arg.Type is TypeNameType { TypeDeclaration: EnumDeclaration enumDecl })
                {
                    // Push number of enum members
                    cg.EmitPushConstInt(enumDecl.Members.Count);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            public override string ToString() => $"INTRINSIC INT COUNT_OF(<array or enum type>)";
        }
    }
}

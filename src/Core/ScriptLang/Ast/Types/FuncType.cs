namespace ScTools.ScriptLang.Ast.Types
{
    using System;
    using System.Linq;

    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.CodeGen;

    public sealed class FuncType : BaseType
    {
        public override int SizeOf => 1;
        public FuncProtoDeclaration Declaration { get; set; }

        public FuncType(SourceRange source, FuncProtoDeclaration declaration) : base(source)
            => Declaration = declaration;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other)
            => other is FuncType otherFunc &&
               Declaration.Kind == otherFunc.Declaration.Kind &&
               Declaration.Parameters.Count == otherFunc.Declaration.Parameters.Count &&
               Declaration.ReturnType.Equivalent(otherFunc.Declaration.ReturnType) &&
               Declaration.Parameters.Zip(otherFunc.Declaration.Parameters).All(p => p.First.Type.Equivalent(p.Second.Type));

        public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs.ByValue is NullType or ErrorType || Equivalent(rhs.ByValue);

        public override IType Invocation((IType Type, bool IsLValue, SourceRange Source)[] args, SourceRange source, DiagnosticsReport diagnostics)
        {
            var parameters = Declaration.Parameters;
            if (args.Length != parameters.Count)
            {
                diagnostics.AddError($"Expected {parameters.Count} arguments, found {args.Length}", source);
            }

            var n = Math.Min(args.Length, parameters.Count);
            for (int i = 0; i < n; i++)
            {
                var param = parameters[i];
                var arg = args[i];
                if (!param.Type.CanAssignInit(arg.Type, arg.IsLValue))
                {
                    diagnostics.AddError($"Argument {i + 1}: cannot pass '{arg.Type}' as parameter '{TypePrinter.ToString(param.Type, param.Name)}'", arg.Source);
                }
            }

            return Declaration.ReturnType;
        }

        public override void CGInvocation(CodeGenerator cg, InvocationExpression expr)
        {
            if (expr.Callee is ValueDeclRefExpression { Declaration: FuncDeclaration func })
            {
                switch (Declaration.Kind)
                {
                    case FuncKind.UserDefined:
                        expr.Arguments.ForEach(e => cg.EmitValue(e));
                        cg.EmitCall(func.Name);
                        break;
                    case FuncKind.Native:
                        expr.Arguments.ForEach(e => cg.EmitValue(e));
                        cg.EmitNativeCall(Declaration.ParametersSize, Declaration.ReturnType.SizeOf, func.Name);
                        break;
                    case FuncKind.Intrinsic: throw new NotImplementedException("intrinsic invocation");
                }
            }
            else
            {
                // TODO: function pointer invocation
                throw new NotImplementedException("function pointer invocation");
            }
        }
    }
}

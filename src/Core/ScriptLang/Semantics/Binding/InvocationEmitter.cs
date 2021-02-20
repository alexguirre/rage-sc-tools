namespace ScTools.ScriptLang.Semantics.Binding
{
    using System;
    using System.Linq;

    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.CodeGen;
    using Type = Semantics.Type;
    using System.Collections.Generic;
    using ScTools.ScriptLang.Semantics.Symbols;
    using System.Diagnostics;

    internal static class InvocationEmitter
    {
        public static void Emit(ByteCodeBuilder code, BoundExpression callee, IEnumerable<BoundExpression> arguments, bool dropReturnValue)
        {
            int returnValueSize = 0;

            if (callee is BoundFunctionExpression funcExpr)
            {
                returnValueSize = funcExpr.Function.Type.ReturnType?.SizeOf ?? 0;
                switch (funcExpr.Function)
                {
                    case NativeFunctionSymbol n:
                        LoadArguments(code, n.Type.Parameters.Select(p => p.Type), arguments);
                        code.EmitNative(n);
                        break;
                    case IntrinsicFunctionSymbol i:
                        i.Emit(code, arguments);
                        break;
                    case DefinedFunctionSymbol d:
                        LoadArguments(code, d.Type.Parameters.Select(p => p.Type), arguments);
                        code.EmitCall(d);
                        break;
                    default: throw new InvalidOperationException("Unknown function symbol");
                }
            }
            else // indirect call
            {
                if (callee.Type is not FunctionType funcTy)
                {
                    throw new InvalidOperationException("Only function types can be called");
                }

                Debug.Assert(funcTy is ExplicitFunctionType, $"Only {nameof(DefinedFunctionSymbol)}s can be called indirectly, so the type should be {nameof(ExplicitFunctionType)}");

                returnValueSize = funcTy.ReturnType?.SizeOf ?? 0;
                LoadArguments(code, (funcTy as ExplicitFunctionType)!.Parameters.Select(p => p.Type), arguments);
                callee.EmitLoad(code);
                code.EmitIndirectCall();
            }

            if (dropReturnValue)
            {
                for (int i = 0; i < returnValueSize; i++)
                {
                    code.Emit(ScriptAssembly.Opcode.DROP);
                }
            }
        }

        private static void LoadArguments(ByteCodeBuilder code, IEnumerable<Type> parameterTypes, IEnumerable<BoundExpression> arguments)
        {
            foreach (var (arg, paramType) in  arguments.Zip(parameterTypes))
            {
                if (paramType is RefType ||
                    (paramType is BasicType { TypeCode: BasicTypeCode.String } && arg.Type?.UnderlyingType is TextLabelType))
                {
                    arg.EmitAddr(code);
                }
                else
                {
                    arg.EmitLoad(code);
                }
            }
        }
    }
}

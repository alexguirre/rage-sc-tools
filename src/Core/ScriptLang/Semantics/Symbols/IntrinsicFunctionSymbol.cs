#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics.Binding;

    /// <summary>
    /// Refers to a function provided by the compiler.
    /// </summary>
    public abstract class IntrinsicFunctionSymbol : FunctionSymbol
    {
        public override FunctionType Type { get; }

        private IntrinsicFunctionSymbol(string name, Type? returnType, int parameterCount) : base(name, SourceRange.Unknown)
            => Type = new IntrinsicFunctionType(returnType, parameterCount, DoesParameterTypeMatch);

        public abstract bool DoesParameterTypeMatch(int parameterIndex, Type? argType);
        public abstract void Emit(ByteCodeBuilder code, IEnumerable<BoundExpression> args);

        public static readonly IntrinsicFunctionSymbol AssignString = new AssignStringSymbol();
        public static readonly IntrinsicFunctionSymbol AssignInt = new AssignIntSymbol();
        public static readonly IntrinsicFunctionSymbol AppendString = new AppendStringSymbol();
        public static readonly IntrinsicFunctionSymbol AppendInt = new AppendIntSymbol();
        public static readonly IntrinsicFunctionSymbol I2F = new I2FSymbol();
        public static readonly IntrinsicFunctionSymbol F2I = new F2ISymbol();
        public static readonly IntrinsicFunctionSymbol F2V = new F2VSymbol();

        private sealed class IntrinsicFunctionType : FunctionType
        {
            public override int ParameterCount { get; }
            public System.Func<int, Type?, bool> DoesParameterTypeMatchCallback { get; }

            public IntrinsicFunctionType(Type? returnType, int parameterCount, System.Func<int, Type?, bool> doesParameterTypeMatch)
                => (ReturnType, ParameterCount, DoesParameterTypeMatchCallback) = (returnType, parameterCount, doesParameterTypeMatch);

            public override bool DoesParameterTypeMatch(int parameterIndex, Type? argType)
                => DoesParameterTypeMatchCallback(parameterIndex, argType);

            public override IntrinsicFunctionType Clone() => new IntrinsicFunctionType(ReturnType, ParameterCount, DoesParameterTypeMatchCallback);

            public override bool Equals(Type? other)
                => other is IntrinsicFunctionType ty &&
                   ParameterCount == ty.ParameterCount &&
                   DoesParameterTypeMatchCallback == ty.DoesParameterTypeMatchCallback;

            public override Type? Resolve(SymbolTable symbols, DiagnosticsReport diagnostics)
                => throw new System.NotImplementedException(nameof(IntrinsicFunctionType) + " does not need to be resolved");

            protected override int DoGetHashCode() => System.HashCode.Combine(ParameterCount, DoesParameterTypeMatchCallback);
            protected override string DoToString() => "<<intrinsic>>";
        }

        private sealed class AssignStringSymbol : IntrinsicFunctionSymbol
        {
            public AssignStringSymbol() : base("ASSIGN_STRING", null, 2) { }

            public override bool DoesParameterTypeMatch(int parameterIndex, Type? argType)
                => CheckTextLabelAndArg(parameterIndex, argType, argIsString: true);

            public override void Emit(ByteCodeBuilder code, IEnumerable<BoundExpression> args)
                => EmitTextLabelAndArg(code, Opcode.TEXT_LABEL_ASSIGN_STRING, args);
        }

        private sealed class AssignIntSymbol : IntrinsicFunctionSymbol
        {
            public AssignIntSymbol() : base("ASSIGN_INT", null, 2) { }

            public override bool DoesParameterTypeMatch(int parameterIndex, Type? argType)
                => CheckTextLabelAndArg(parameterIndex, argType, argIsString: false);

            public override void Emit(ByteCodeBuilder code, IEnumerable<BoundExpression> args)
                => EmitTextLabelAndArg(code, Opcode.TEXT_LABEL_ASSIGN_INT, args);
        }

        private sealed class AppendStringSymbol : IntrinsicFunctionSymbol
        {
            public AppendStringSymbol() : base("APPEND_STRING", null, 2) { }

            public override bool DoesParameterTypeMatch(int parameterIndex, Type? argType)
                => CheckTextLabelAndArg(parameterIndex, argType, argIsString: true);

            public override void Emit(ByteCodeBuilder code, IEnumerable<BoundExpression> args)
                => EmitTextLabelAndArg(code, Opcode.TEXT_LABEL_APPEND_STRING, args);
        }

        private sealed class AppendIntSymbol : IntrinsicFunctionSymbol
        {
            public AppendIntSymbol() : base("APPEND_INT", null, 2) { }

            public override bool DoesParameterTypeMatch(int parameterIndex, Type? argType)
                => CheckTextLabelAndArg(parameterIndex, argType, argIsString: false);

            public override void Emit(ByteCodeBuilder code, IEnumerable<BoundExpression> args)
                => EmitTextLabelAndArg(code, Opcode.TEXT_LABEL_APPEND_INT, args);
        }

        private static void EmitTextLabelAndArg(ByteCodeBuilder code, Opcode opcode, IEnumerable<BoundExpression> args)
        {
            var dstArg = args.ElementAt(0);
            var srcArg = args.ElementAt(1);
            
            var dstTextLabelTy = (TextLabelType)dstArg.Type!.UnderlyingType;

            if (srcArg.Type?.UnderlyingType is TextLabelType)
            {
                srcArg.EmitAddr(code); // src text label, convert to string
            }
            else
            {
                srcArg.EmitLoad(code); // src string/int
            }
            dstArg.EmitAddr(code); // dest text label
            code.Emit(opcode, new[] { new Operand(unchecked((uint)dstTextLabelTy.Length)) });
        }

        private static bool CheckTextLabelAndArg(int parameterIndex, Type? argType, bool argIsString)
        {
            if (parameterIndex == 0) // dst parameter
            {
                return argType?.UnderlyingType is TextLabelType;
            }
            else if (parameterIndex == 1) // src parameter
            {
                return argIsString ?
                        (argType != null && BuiltInTypes.STRING.IsAssignableFrom(argType, considerReferences: true)) :
                        (argType != null && BuiltInTypes.INT.IsAssignableFrom(argType, considerReferences: true));
            }
            else
            {
                return false;
            }
        }

        private sealed class I2FSymbol : IntrinsicFunctionSymbol
        {
            public I2FSymbol() : base("I2F", BuiltInTypes.FLOAT, 1) { }

            public override bool DoesParameterTypeMatch(int parameterIndex, Type? argType)
                => parameterIndex == 0 && argType != null && BuiltInTypes.INT.IsAssignableFrom(argType, considerReferences: true);

            public override void Emit(ByteCodeBuilder code, IEnumerable<BoundExpression> args)
            {
                args.Single().EmitLoad(code);
                code.Emit(Opcode.I2F);
            }
        }

        private sealed class F2ISymbol : IntrinsicFunctionSymbol
        {
            public F2ISymbol() : base("F2I", BuiltInTypes.INT, 1) { }

            public override bool DoesParameterTypeMatch(int parameterIndex, Type? argType)
                => parameterIndex == 0 && argType != null && BuiltInTypes.FLOAT.IsAssignableFrom(argType, considerReferences: true);

            public override void Emit(ByteCodeBuilder code, IEnumerable<BoundExpression> args)
            {
                args.Single().EmitLoad(code);
                code.Emit(Opcode.F2I);
            }
        }

        private sealed class F2VSymbol : IntrinsicFunctionSymbol
        {
            public F2VSymbol() : base("F2V", BuiltInTypes.VECTOR, 1) { }

            public override bool DoesParameterTypeMatch(int parameterIndex, Type? argType)
                => parameterIndex == 0 && argType != null && BuiltInTypes.FLOAT.IsAssignableFrom(argType, considerReferences: true);

            public override void Emit(ByteCodeBuilder code, IEnumerable<BoundExpression> args)
            {
                args.Single().EmitLoad(code);
                code.Emit(Opcode.F2V);
            }
        }
    }
}

#nullable enable
namespace ScTools.ScriptLang.Semantics.Symbols
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.CodeGen;
    using ScTools.ScriptLang.Semantics.Binding;

    public abstract class IntrinsicFunctionSymbol : ISymbol
    {
        public string Name { get; }
        public SourceRange Source => SourceRange.Unknown;

        public IntrinsicFunctionSymbol(string name)
            => Name = name;

        public abstract void CheckArguments(IEnumerable<(Type?, SourceRange)> argTypes, DiagnosticsReport diagnostics, string filePath);
        public abstract void Emit(ByteCodeBuilder code, ImmutableArray<BoundExpression> args);

        public static readonly IntrinsicFunctionSymbol AssignString = new AssignStringSymbol();
        public static readonly IntrinsicFunctionSymbol AssignInt = new AssignIntSymbol();
        public static readonly IntrinsicFunctionSymbol AppendString = new AppendStringSymbol();
        public static readonly IntrinsicFunctionSymbol AppendInt = new AppendIntSymbol();

        private static void CheckArgumentsTextLabelAndArg(IEnumerable<(Type?, SourceRange)> argTypes, bool argIsString, DiagnosticsReport diagnostics, string filePath)
        {
            int n = 0;
            foreach (var (ty, source) in argTypes)
            {
                if (n > 2)
                {
                    n++;
                    continue;
                }

                switch (n)
                {
                    case 0 when ty?.UnderlyingType is not TextLabelType:
                        diagnostics.AddError(filePath, $"Mismatched type of argument #{n}, expected TEXT_LABEL", source);
                        break;
                    case 1 when argIsString && (ty == null || !new BasicType(BasicTypeCode.String).IsAssignableFrom(ty, true)):
                        diagnostics.AddError(filePath, $"Mismatched type of argument #{n}, expected STRING", source);
                        break;
                    case 1 when !argIsString && (ty == null || !new BasicType(BasicTypeCode.Int).IsAssignableFrom(ty, true)):
                        diagnostics.AddError(filePath, $"Mismatched type of argument #{n}, expected INT", source);
                        break;
                }

                n++;
            }

            if (n != 2)
            {
                diagnostics.AddError(filePath, $"Mismatched number of arguments. Expected 2, found {n}", SourceRange.Unknown/*TODO*/);
            }
        }

        private static void EmitTextLabelAndArg(ByteCodeBuilder code, Opcode opcode, ImmutableArray<BoundExpression> args)
        {
            Debug.Assert(args.Length == 2);

            var textLabelTy = (TextLabelType)args[0].Type!.UnderlyingType;

            if (args[1].Type?.UnderlyingType is TextLabelType)
            {
                args[1].EmitAddr(code); // src text label, convert to string
            }
            else
            {
                args[1].EmitLoad(code); // src string/int
            }
            args[0].EmitAddr(code); // dest text label
            code.Emit(opcode, new[] { new Operand(unchecked((uint)textLabelTy.Length)) });
        }

        private sealed class AssignStringSymbol : IntrinsicFunctionSymbol
        {
            public AssignStringSymbol() : base("ASSIGN_STRING") { }

            public override void CheckArguments(IEnumerable<(Type?, SourceRange)> argTypes, DiagnosticsReport diagnostics, string filePath)
                => CheckArgumentsTextLabelAndArg(argTypes, argIsString: true, diagnostics, filePath);

            public override void Emit(ByteCodeBuilder code, ImmutableArray<BoundExpression> args)
                => EmitTextLabelAndArg(code, Opcode.TEXT_LABEL_ASSIGN_STRING, args);
        }

        private sealed class AssignIntSymbol : IntrinsicFunctionSymbol
        {
            public AssignIntSymbol() : base("ASSIGN_INT") { }

            public override void CheckArguments(IEnumerable<(Type?, SourceRange)> argTypes, DiagnosticsReport diagnostics, string filePath)
                => CheckArgumentsTextLabelAndArg(argTypes, argIsString: false, diagnostics, filePath);

            public override void Emit(ByteCodeBuilder code, ImmutableArray<BoundExpression> args)
                => EmitTextLabelAndArg(code, Opcode.TEXT_LABEL_ASSIGN_INT, args);
        }

        private sealed class AppendStringSymbol : IntrinsicFunctionSymbol
        {
            public AppendStringSymbol() : base("APPEND_STRING") { }

            public override void CheckArguments(IEnumerable<(Type?, SourceRange)> argTypes, DiagnosticsReport diagnostics, string filePath)
                => CheckArgumentsTextLabelAndArg(argTypes, argIsString: true, diagnostics, filePath);

            public override void Emit(ByteCodeBuilder code, ImmutableArray<BoundExpression> args)
                => EmitTextLabelAndArg(code, Opcode.TEXT_LABEL_APPEND_STRING, args);
        }

        private sealed class AppendIntSymbol : IntrinsicFunctionSymbol
        {
            public AppendIntSymbol() : base("APPEND_INT") { }

            public override void CheckArguments(IEnumerable<(Type?, SourceRange)> argTypes, DiagnosticsReport diagnostics, string filePath)
                => CheckArgumentsTextLabelAndArg(argTypes, argIsString: false, diagnostics, filePath);

            public override void Emit(ByteCodeBuilder code, ImmutableArray<BoundExpression> args)
                => EmitTextLabelAndArg(code, Opcode.TEXT_LABEL_APPEND_INT, args);
        }
    }
}

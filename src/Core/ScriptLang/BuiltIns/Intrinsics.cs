namespace ScTools.ScriptLang.BuiltIns
{
    using System;
    using System.Linq;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Types;

    public static class Intrinsics
    {
        private static readonly StringOrIntDecl StringOrInt = new();
        private static readonly GenericTextLabelRefDecl GenericTextLabelRef = new();

        public static FuncDeclaration F2V { get; } = CreateFunc("F2V", BuiltInTypes.Vector, (BuiltInTypes.Float, "value"));
        public static FuncDeclaration F2I { get; } = CreateFunc("F2I", BuiltInTypes.Int, (BuiltInTypes.Float, "value"));
        public static FuncDeclaration I2F { get; } = CreateFunc("I2F", BuiltInTypes.Float, (BuiltInTypes.Int, "value"));
        public static FuncDeclaration Assign { get; } = CreateProc("ASSIGN", (GenericTextLabelRef, "tl"), (StringOrInt, "value"));
        public static FuncDeclaration Append { get; } = CreateProc("APPEND", (GenericTextLabelRef, "tl"), (StringOrInt, "value"));

        private static FuncDeclaration CreateProc(string name, params (ITypeDeclaration Type, string Name)[] parameters)
            => CreateFunc(name, new VoidType(SourceRange.Unknown), parameters);

        private static FuncDeclaration CreateFunc(string name, ITypeDeclaration returnType, params (ITypeDeclaration Type, string Name)[] parameters)
            => CreateFunc(name, returnType.CreateType(SourceRange.Unknown), parameters);

        private static FuncDeclaration CreateFunc(string name, IType returnType, params (ITypeDeclaration Type, string Name)[] parameters)
            => new(SourceRange.Unknown, name,
                new(SourceRange.Unknown, name + "@proto", FuncKind.Intrinsic, returnType)
                {
                    Parameters = parameters.Select(p => new VarDeclaration(SourceRange.Unknown, p.Name, p.Type.CreateType(SourceRange.Unknown), VarKind.Parameter)).ToList()
                });

        /// <summary>
        /// Used for parameters that can be STRING or INT.
        /// </summary>
        private sealed class StringOrIntDecl : BaseTypeDeclaration
        {
            public StringOrIntDecl() : base(SourceRange.Unknown, "STRING|INT") { }
            public override StringOrIntType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class StringOrIntType : BaseType
        {
            public override int SizeOf => 0;

            public StringType String { get; }
            public IntType Int { get; }

            public StringOrIntType(SourceRange source) : base(source)
                => (String, Int) = (new(source), new(source));

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
            public override bool Equivalent(IType other) => other is StringOrIntType;

            public override bool CanAssignInit(IType rhs, bool isLValue)
                => String.CanAssignInit(rhs, isLValue) || Int.CanAssignInit(rhs, isLValue);

            public override string ToString() => "STRING|INT";
        }

        /// <summary>
        /// Used for parameters that can be a reference to any TEXT_LABEL_*.
        /// </summary>
        private sealed class GenericTextLabelRefDecl : BaseTypeDeclaration
        {
            public GenericTextLabelRefDecl() : base(SourceRange.Unknown, "TEXT_LABEL_*&") { }
            public override RefType CreateType(SourceRange source) => new(source, new GenericTextLabelType(source));
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class GenericTextLabelType : BaseType
        {
            public override int SizeOf => 0;

            public GenericTextLabelType(SourceRange source) : base(source) { }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
            public override bool Equivalent(IType other) => other is GenericTextLabelType;

            // incomplete array types can reference arrays of any size if their item types are equivalent
            public override bool CanBindRefTo(IType other)
                => other is TextLabelType;

            public override string ToString() => "TEXT_LABEL_*";
        }
    }
}

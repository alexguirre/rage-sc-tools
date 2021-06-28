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
        private static readonly GenericTextLabelDecl GenericTextLabel = new();
        private static readonly GenericArrayDecl GenericArray = new();

        public static FuncDeclaration F2V { get; } = CreateFunc("F2V", BuiltInTypes.Vector, (BuiltInTypes.Float, "value", false));
        public static FuncDeclaration F2I { get; } = CreateFunc("F2I", BuiltInTypes.Int, (BuiltInTypes.Float, "value", false));
        public static FuncDeclaration I2F { get; } = CreateFunc("I2F", BuiltInTypes.Float, (BuiltInTypes.Int, "value", false));
        public static FuncDeclaration Append { get; } = CreateProc("APPEND", (GenericTextLabel, "tl", true), (StringOrInt, "value", false));
        public static FuncDeclaration ArraySize { get; } = CreateFunc("ARRAY_SIZE", BuiltInTypes.Int, (GenericArray, "array", true));

        private static FuncDeclaration CreateProc(string name, params (ITypeDeclaration Type, string Name, bool IsRef)[] parameters)
            => CreateFunc(name, new VoidType(SourceRange.Unknown), parameters);

        private static FuncDeclaration CreateFunc(string name, ITypeDeclaration returnType, params (ITypeDeclaration Type, string Name, bool IsRef)[] parameters)
            => CreateFunc(name, returnType.CreateType(SourceRange.Unknown), parameters);

        private static FuncDeclaration CreateFunc(string name, IType returnType, params (ITypeDeclaration Type, string Name, bool IsRef)[] parameters)
            => new(SourceRange.Unknown, name,
                new(SourceRange.Unknown, name + "@proto", FuncKind.Intrinsic, returnType)
                {
                    Parameters = parameters.Select(p => new VarDeclaration(SourceRange.Unknown, p.Name, p.Type.CreateType(SourceRange.Unknown), VarKind.Parameter, p.IsRef)).ToList()
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

            public override bool CanAssign(IType rhs, bool rhsIsLValue)
                => String.CanAssign(rhs, rhsIsLValue) || Int.CanAssign(rhs, rhsIsLValue);

            public override string ToString() => "STRING|INT";
        }

        /// <summary>
        /// Used for parameters that can be a reference to any TEXT_LABEL_*.
        /// </summary>
        private sealed class GenericTextLabelDecl : BaseTypeDeclaration
        {
            public GenericTextLabelDecl() : base(SourceRange.Unknown, "TEXT_LABEL_*") { }
            public override GenericTextLabelType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class GenericTextLabelType : BaseType
        {
            public override int SizeOf => 0;

            public GenericTextLabelType(SourceRange source) : base(source) { }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
            public override bool Equivalent(IType other) => other is GenericTextLabelType;

            public override bool CanBindRefTo(IType other)
                => other is TextLabelType;

            public override string ToString() => "TEXT_LABEL_*";
        }

        /// <summary>
        /// Used for parameters that can be a reference to any array.
        /// </summary>
        private sealed class GenericArrayDecl : BaseTypeDeclaration
        {
            public GenericArrayDecl() : base(SourceRange.Unknown, "<array>") { }
            public override GenericArrayType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class GenericArrayType : BaseType, IArrayType
        {
            public override int SizeOf => 0;

            public IType ItemType { get; set; }

            public GenericArrayType(SourceRange source) : base(source) => ItemType = new GenericType(source);

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
            public override bool Equivalent(IType other) => other is GenericArrayType;

            public override bool CanBindRefTo(IType other)
                => other is IArrayType;
        }

        private sealed class GenericType : BaseType
        {
            public override int SizeOf => 0;

            public GenericType(SourceRange source) : base(source) { }

            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
            public override bool Equivalent(IType other) => other is GenericType;

            public override bool CanBindRefTo(IType other) => true;

            public override string ToString() => "<any type>";
        }
    }
}

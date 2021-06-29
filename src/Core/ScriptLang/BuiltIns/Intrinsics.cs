namespace ScTools.ScriptLang.BuiltIns
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using ScTools.ScriptAssembly;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;
    using ScTools.ScriptLang.CodeGen;

    public interface IIntrinsic
    {
        FuncDeclaration Declaration { get; }
        void Emit(CodeGenerator cg, IList<IExpression> args);
    }

    public static class Intrinsics
    {
        private static readonly StringOrIntDecl StringOrInt = new();
        private static readonly GenericTextLabelDecl GenericTextLabel = new();
        private static readonly GenericArrayDecl GenericArray = new();

        public static IIntrinsic F2V { get; } = new F2VIntrinsic();
        public static IIntrinsic F2I { get; } = new F2IIntrinsic();
        public static IIntrinsic I2F { get; } = new I2FIntrinsic();
        public static IIntrinsic Append { get; } = new AppendIntrinsic();
        public static IIntrinsic ArraySize { get; } = new ArraySizeIntrinsic();

        public static ImmutableArray<IIntrinsic> AllIntrinsics { get; } = ImmutableArray.Create(F2V, F2I, I2F, Append, ArraySize);

        public static IIntrinsic? FindIntrinsic(string name) => AllIntrinsics.FirstOrDefault(i => Parser.CaseInsensitiveComparer.Equals(name, i.Declaration.Name));

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

        private sealed class F2VIntrinsic : IIntrinsic
        {
            public FuncDeclaration Declaration { get; } = CreateFunc("F2V", BuiltInTypes.Vector, (BuiltInTypes.Float, "value", false));

            public void Emit(CodeGenerator cg, IList<IExpression> args)
            {
                cg.EmitValue(args[0]);
                cg.Emit(Opcode.F2V);
            }
        }

        private sealed class F2IIntrinsic : IIntrinsic
        {
            public FuncDeclaration Declaration { get; } = CreateFunc("F2I", BuiltInTypes.Int, (BuiltInTypes.Float, "value", false));

            public void Emit(CodeGenerator cg, IList<IExpression> args)
            {
                cg.EmitValue(args[0]);
                cg.Emit(Opcode.F2I);
            }
        }

        private sealed class I2FIntrinsic : IIntrinsic
        {
            public FuncDeclaration Declaration { get; } = CreateFunc("I2F", BuiltInTypes.Float, (BuiltInTypes.Int, "value", false));

            public void Emit(CodeGenerator cg, IList<IExpression> args)
            {
                cg.EmitValue(args[0]);
                cg.Emit(Opcode.I2F);
            }
        }

        private sealed class AppendIntrinsic : IIntrinsic
        {
            public FuncDeclaration Declaration { get; } = CreateProc("APPEND", (GenericTextLabel, "tl", true), (StringOrInt, "value", false));

            public void Emit(CodeGenerator cg, IList<IExpression> args) => throw new NotImplementedException();
        }

        private sealed class ArraySizeIntrinsic : IIntrinsic
        {
            public FuncDeclaration Declaration { get; } = CreateFunc("ARRAY_SIZE", BuiltInTypes.Int, (GenericArray, "array", true));

            public void Emit(CodeGenerator cg, IList<IExpression> args)
            {
                var array = args[0];
                var arrayTy = (IArrayType)array.Type!;
                if (arrayTy is ArrayType constantSizeArrayTy)
                {
                    // Size known at compile-time
                    cg.EmitPushConstInt(constantSizeArrayTy.Length);
                }
                else
                {
                    // Size known at runtime. The size is store at offset 0 of the array
                    cg.EmitAddress(array);
                    cg.Emit(Opcode.LOAD);
                }
            }
        }

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

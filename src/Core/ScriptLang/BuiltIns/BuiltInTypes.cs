namespace ScTools.ScriptLang.BuiltIns
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Types;

    public static class BuiltInTypes
    {
        public static ITypeDeclaration Int { get; } = new IntDecl();
        public static ITypeDeclaration Float { get; } = new FloatDecl();
        public static ITypeDeclaration Bool { get; } = new BoolDecl();
        public static ITypeDeclaration String { get; } = new StringDecl();
        public static ITypeDeclaration Any { get; } = new AnyDecl();
        public static ITypeDeclaration Vector { get; } = new VectorDecl();
        public static ITypeDeclaration PlayerIndex { get; } = new HandleDecl(HandleKind.PlayerIndex);
        public static ITypeDeclaration EntityIndex { get; } = new HandleDecl(HandleKind.EntityIndex);
        public static ITypeDeclaration PedIndex { get; } = new HandleDecl(HandleKind.PedIndex);
        public static ITypeDeclaration VehicleIndex { get; } = new HandleDecl(HandleKind.VehicleIndex);
        public static ITypeDeclaration ObjectIndex { get; } = new HandleDecl(HandleKind.ObjectIndex);
        public static ITypeDeclaration CameraIndex { get; } = new HandleDecl(HandleKind.CameraIndex);
        public static ITypeDeclaration PickupIndex { get; } = new HandleDecl(HandleKind.PickupIndex);
        public static ITypeDeclaration BlipInfoId { get; } = new HandleDecl(HandleKind.BlipInfoId);
        public static ITypeDeclaration[] TextLabels { get; } = CreateTextLabels();

        private static ITypeDeclaration[] CreateTextLabels()
        {
            const int Count = (TextLabelType.MaxLength - TextLabelType.MinLength) / 8 + 1;
            var arr = new TextLabelDecl[Count];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = new TextLabelDecl((i + 1) * 8);
            }
            return arr;
        }

        // TODO: should these be public ITypeDeclarations?
        private sealed class IntDecl : BaseTypeDeclaration
        {
            public IntDecl() : base(SourceRange.Unknown, "INT") { }
            public override IntType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class FloatDecl : BaseTypeDeclaration
        {
            public FloatDecl() : base(SourceRange.Unknown, "FLOAT") { }
            public override FloatType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class BoolDecl : BaseTypeDeclaration
        {
            public BoolDecl() : base(SourceRange.Unknown, "BOOL") { }
            public override BoolType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class StringDecl : BaseTypeDeclaration
        {
            public StringDecl() : base(SourceRange.Unknown, "STRING") { }
            public override StringType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class AnyDecl : BaseTypeDeclaration
        {
            public AnyDecl() : base(SourceRange.Unknown, "ANY") { }
            public override AnyType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class TextLabelDecl : BaseTypeDeclaration
        {
            public int Length { get; }
            public TextLabelDecl(int length) : base(SourceRange.Unknown, $"TEXT_LABEL_{length-1}") // real type name found in tty scripts from RDR3 (e.g. 'TEXT_LABEL_63 tlDebugName', 'TEXT_LABEL_31 tlPlaylist' or 'XML_LOADER_GET_TEXT_LABEL_127_RQ')
            {
                Debug.Assert(TextLabelType.IsValidLength(length));
                Length = length;
            }
            public override TextLabelType CreateType(SourceRange source) => new(source, Length);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class VectorDecl : BaseTypeDeclaration
        {
            public VectorDecl() : base(SourceRange.Unknown, "VECTOR") { }
            public override VectorType CreateType(SourceRange source) => new(source);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }

        private sealed class HandleDecl : BaseTypeDeclaration
        {
            public HandleKind Kind { get; }
            public HandleDecl(HandleKind kind) : base(SourceRange.Unknown, HandleType.KindToTypeName(kind)) => Kind = kind;
            public override HandleType CreateType(SourceRange source) => new(source, Kind);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }
    }
}

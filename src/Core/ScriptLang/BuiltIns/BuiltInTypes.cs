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
        private static readonly HashSet<StructDeclaration> HandleTypes = new();

        public static  ITypeDeclaration Int { get; } = new IntDecl();
        public static  ITypeDeclaration Float { get; } = new FloatDecl();
        public static  ITypeDeclaration Bool { get; } = new BoolDecl();
        public static  ITypeDeclaration String { get; } = new StringDecl();
        public static  ITypeDeclaration Any { get; } = new AnyDecl();
        public static  StructDeclaration Vector { get; } = new(SourceRange.Unknown, "VECTOR")
        {
            Fields = new List<StructField>
                {
                    new StructField(SourceRange.Unknown, "x", Float.CreateType(SourceRange.Unknown)),
                    new StructField(SourceRange.Unknown, "y", Float.CreateType(SourceRange.Unknown)),
                    new StructField(SourceRange.Unknown, "z", Float.CreateType(SourceRange.Unknown)),
                },
        };
        public static StructDeclaration PlayerIndex { get; } = CreateHandleType("PLAYER_INDEX");
        public static StructDeclaration EntityIndex { get; } = CreateHandleType("ENTITY_INDEX");
        public static StructDeclaration PedIndex { get; } = CreateHandleType("PED_INDEX");
        public static StructDeclaration VehicleIndex { get; } = CreateHandleType("VEHICLE_INDEX");
        public static StructDeclaration ObjectIndex { get; } = CreateHandleType("OBJECT_INDEX");
        public static StructDeclaration CameraIndex { get; } = CreateHandleType("CAMERA_INDEX");
        public static StructDeclaration PickupIndex { get; } = CreateHandleType("PICKUP_INDEX");
        public static StructDeclaration BlipInfoId { get; } = CreateHandleType("BLIP_INFO_ID");
        public static ITypeDeclaration[] TextLabels { get; } = CreateTextLabels();

        public static bool IsHandleType(IType type)
            => type is StructType structTy && HandleTypes.Contains(structTy.Declaration);

        public static bool IsVectorType(IType type)
            => type is StructType structTy && structTy.Declaration == Vector;

        private static StructDeclaration CreateHandleType(string name)
        {
            var decl = new StructDeclaration(SourceRange.Unknown, name)
            {
                Fields = new List<StructField>
                {
                    new StructField(SourceRange.Unknown, "value", Int.CreateType(SourceRange.Unknown)),
                },
            };
            HandleTypes.Add(decl);
            return decl;
        }

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
            public TextLabelDecl(int length) : base(SourceRange.Unknown, $"TEXT_LABEL{length}")
            {
                Debug.Assert(TextLabelType.IsValidLength(length));
                Length = length;
            }
            public override TextLabelType CreateType(SourceRange source) => new(source, Length);
            public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param) => throw new NotImplementedException();
        }
    }
}

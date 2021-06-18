namespace ScTools.ScriptLang.SymbolTables
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Types;

    /// <summary>
    /// Table with symbols available anywhere in the script (i.e. built-ins, enums, structs, functions, procedures and non-local variables).
    /// </summary>
    public sealed class GlobalSymbolTable
    {
        private readonly Dictionary<string, ITypeDeclaration> typeDeclarations = new(Parser.CaseInsensitiveComparer);
        private readonly Dictionary<string, IValueDeclaration> valueDeclarations = new(Parser.CaseInsensitiveComparer);

        public ITypeDeclaration Int { get; } = new IntDecl();
        public ITypeDeclaration Float { get; } = new FloatDecl();
        public ITypeDeclaration Bool { get; } = new BoolDecl();
        public ITypeDeclaration String { get; } = new StringDecl();
        public ITypeDeclaration Any { get; } = new AnyDecl();
        public StructDeclaration Vector { get; } = new(SourceRange.Unknown, "VECTOR")
            {
                Fields = new List<StructField>
                {
                    new StructField(SourceRange.Unknown, "x", new FloatType(SourceRange.Unknown)),
                    new StructField(SourceRange.Unknown, "y", new FloatType(SourceRange.Unknown)),
                    new StructField(SourceRange.Unknown, "z", new FloatType(SourceRange.Unknown)),
                },
            };
        public StructDeclaration PlayerIndex { get; } = CreateHandleType("PLAYER_INDEX");
        public StructDeclaration EntityIndex { get; } = CreateHandleType("ENTITY_INDEX");
        public StructDeclaration PedIndex { get; } = CreateHandleType("PED_INDEX");
        public StructDeclaration VehicleIndex { get; } = CreateHandleType("VEHICLE_INDEX");
        public StructDeclaration ObjectIndex { get; } = CreateHandleType("OBJECT_INDEX");
        public StructDeclaration CameraIndex { get; } = CreateHandleType("CAMERA_INDEX");
        public StructDeclaration PickupIndex { get; } = CreateHandleType("PICKUP_INDEX");
        public StructDeclaration BlipInfoId { get; } = CreateHandleType("BLIP_INFO_ID");
        public ITypeDeclaration[] TextLabels { get; } = CreateTextLabels();

        public GlobalSymbolTable()
        {
            AddBuiltIns();
        }

        public bool AddType(ITypeDeclaration typeDeclaration) => typeDeclarations.TryAdd(typeDeclaration.Name, typeDeclaration);
        public bool AddValue(IValueDeclaration valueDeclaration) => valueDeclarations.TryAdd(valueDeclaration.Name, valueDeclaration);

        public ITypeDeclaration? FindType(string name) => typeDeclarations.TryGetValue(name, out var decl) ? decl : null;
        public IValueDeclaration? FindValue(string name) => valueDeclarations.TryGetValue(name, out var decl) ? decl : null;

        private void AddBuiltIns()
        {
            AddType(Int);
            AddType(Float);
            AddType(Bool);
            AddType(String);
            AddType(Any);
            AddType(PlayerIndex);
            AddType(EntityIndex);
            AddType(PedIndex);
            AddType(VehicleIndex);
            AddType(ObjectIndex);
            AddType(CameraIndex);
            AddType(PickupIndex);
            AddType(BlipInfoId);
            TextLabels.ForEach(lbl => AddType(lbl));
        }

        private static StructDeclaration CreateHandleType(string name)
            => new(SourceRange.Unknown, name)
            {
                Fields = new List<StructField>
                {
                    new StructField(SourceRange.Unknown, "value", new IntType(SourceRange.Unknown)),
                },
            };

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

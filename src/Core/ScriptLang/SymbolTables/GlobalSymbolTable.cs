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
            AddType(new IntDecl());
            AddType(new FloatDecl());
            AddType(new BoolDecl());
            AddType(new StringDecl());
            AddType(new AnyDecl());
            AddType(new StructDeclaration(SourceRange.Unknown, "VECTOR")
            {
                Fields = new List<StructField>
                {
                    new StructField(SourceRange.Unknown, "x", new FloatType(SourceRange.Unknown)),
                    new StructField(SourceRange.Unknown, "y", new FloatType(SourceRange.Unknown)),
                    new StructField(SourceRange.Unknown, "z", new FloatType(SourceRange.Unknown)),
                },
            });
            AddHandleType("PLAYER_INDEX");
            AddHandleType("ENTITY_INDEX");
            AddHandleType("PED_INDEX");
            AddHandleType("VEHICLE_INDEX");
            AddHandleType("OBJECT_INDEX");
            AddHandleType("CAMERA_INDEX");
            AddHandleType("PICKUP_INDEX");
            AddHandleType("BLIP_INFO_ID");
            for (int length = TextLabelType.MinLength; length <= TextLabelType.MaxLength; length += 8)
            {
                AddType(new TextLabelDecl(length));
            }


            void AddHandleType(string name)
                => AddType(new StructDeclaration(SourceRange.Unknown, name)
                {
                    Fields = new List<StructField>
                    {
                        new StructField(SourceRange.Unknown, "value", new IntType(SourceRange.Unknown)),
                    },
                });
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

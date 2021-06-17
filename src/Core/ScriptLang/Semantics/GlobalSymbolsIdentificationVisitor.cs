namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Types;

    public sealed class SymbolTable
    {
        public static StringComparer CaseInsensitiveComparer => ScriptAssembly.Assembler.CaseInsensitiveComparer;

        public List<ITypeDeclaration> typeDeclarations = new();
        public List<IValueDeclaration> valueDeclarations = new();

        public SymbolTable()
        {
            AddBuiltIns();
        }

        public void AddType(ITypeDeclaration typeDeclaration) => typeDeclarations.Add(typeDeclaration);
        public void AddValue(IValueDeclaration valueDeclaration) => valueDeclarations.Add(valueDeclaration);

        public ITypeDeclaration? FindTypeDecl(string name) => typeDeclarations.Find(decl => CaseInsensitiveComparer.Compare(decl.Name, name) == 0);

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

    /// <summary>
    /// Fills the symbol table with enums, structs, functions, procedures and non-local variables.
    /// </summary>
    public sealed class GlobalSymbolsIdentificationVisitor : DFSVisitor<Void, Void>
    {
        public override Void DefaultReturn => default;

        public SymbolTable Symbols { get; } = new();

        public override Void Visit(EnumDeclaration node, Void param)
        {
            Symbols.AddType(node);
            return DefaultReturn;
        }

        public override Void Visit(EnumMemberDeclaration node, Void param)
        {
            Symbols.AddValue(node);
            return DefaultReturn;
        }

        public override Void Visit(FuncDeclaration node, Void param)
        {
            Symbols.AddValue(node);
            return DefaultReturn;
        }

        public override Void Visit(FuncProtoDeclaration node, Void param)
        {
            Symbols.AddType(node);
            return DefaultReturn;
        }

        public override Void Visit(StructDeclaration node, Void param)
        {
            Symbols.AddType(node);
            return DefaultReturn;
        }

        public override Void Visit(VarDeclaration node, Void param)
        {
            Debug.Assert(node.Kind is VarKind.Constant or VarKind.Global or VarKind.Static or VarKind.StaticArg);
            Symbols.AddValue(node);
            return DefaultReturn;
        }
    }

    /// <summary>
    /// Empty struct used by <see cref="IVisitor{TReturn, TParam}"/> implementations that do not require <c>TReturn</c> or <c>TParam</c>.
    /// </summary>
    public struct Void { }
}

namespace ScTools.ScriptLang.Semantics
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;

    public sealed class SymbolTable
    {
        public List<ITypeDeclaration> typeDeclarations = new();
        public List<IValueDeclaration> valueDeclarations = new();

        public void AddType(ITypeDeclaration typeDeclaration) => typeDeclarations.Add(typeDeclaration);
        public void AddValue(IValueDeclaration valueDeclaration) => valueDeclarations.Add(valueDeclaration);
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

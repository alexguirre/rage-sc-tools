namespace ScTools.ScriptLang.Semantics
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.SymbolTables;

    /// <summary>
    /// Fills the symbol table with enums, structs, functions, procedures and non-local variables.
    /// </summary>
    public sealed class GlobalSymbolTableBuilder : DFSVisitor
    {
        public DiagnosticsReport Diagnostics { get; }
        public GlobalSymbolTable Symbols { get; } = new();

        private GlobalSymbolTableBuilder(DiagnosticsReport diagnostics)
            => Diagnostics = diagnostics;

        public override Void Visit(EnumDeclaration node, Void param)
        {
            AddTypeChecked(node);
            return base.Visit(node, param);
        }

        public override Void Visit(EnumMemberDeclaration node, Void param)
        {
            AddValueChecked(node);
            return DefaultReturn;
        }

        public override Void Visit(FuncDeclaration node, Void param)
        {
            AddValueChecked(node);
            return DefaultReturn;
        }

        public override Void Visit(FuncProtoDeclaration node, Void param)
        {
            AddTypeChecked(node);
            return DefaultReturn;
        }

        public override Void Visit(StructDeclaration node, Void param)
        {
            AddTypeChecked(node);
            return DefaultReturn;
        }

        public override Void Visit(VarDeclaration node, Void param)
        {
            Debug.Assert(node.Kind is VarKind.Constant or VarKind.Global or VarKind.Static or VarKind.StaticArg);
            AddValueChecked(node);
            return DefaultReturn;
        }

        private void AddTypeChecked(ITypeDeclaration node)
        {
            if (!Symbols.AddType(node))
            {
                Diagnostics.AddError($"Type symbol '{node.Name}' is already declared", node.Source);
            }
        }

        private void AddValueChecked(IValueDeclaration node)
        {
            if (!Symbols.AddValue(node))
            {
                Diagnostics.AddError($"Symbol '{node.Name}' is already declared", node.Source);
            }
        }

        public static GlobalSymbolTable Build(Program root, DiagnosticsReport diagnostics)
        {
            var visitor = new GlobalSymbolTableBuilder(diagnostics);
            root.Accept(visitor, default);
            return visitor.Symbols;
        }
    }
}

namespace ScTools.ScriptLang.Semantics
{
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.SymbolTables;

    /// <summary>
    /// Fills the symbol table with all the labels inside a function or procedure.
    /// </summary>
    public sealed class LabelSymbolTableBuilder : DFSVisitor
    {
        public DiagnosticsReport Diagnostics { get; }
        public LabelSymbolTable Labels { get; } = new();

        private LabelSymbolTableBuilder(DiagnosticsReport diagnostics)
            => Diagnostics = diagnostics;

        public override Void Visit(LabelDeclaration node, Void param)
        {
            if (!Labels.AddLabel(node))
            {
                Diagnostics.AddError($"Label '{node.Name}' is already declared", node.Source);
            }

            return DefaultReturn;
        }

        public static LabelSymbolTable Build(FuncDeclaration func, DiagnosticsReport diagnostics)
        {
            var visitor = new LabelSymbolTableBuilder(diagnostics);
            func.Accept(visitor, default);
            return visitor.Labels;
        }
    }
}

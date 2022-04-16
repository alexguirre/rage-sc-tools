#if false
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Statements;
    using ScTools.ScriptLang.SymbolTables;

    /// <summary>
    /// Fills the symbol table with all the labels inside a function or procedure.
    /// </summary>
    public sealed class LabelSymbolTableBuilder : DFSVisitor
    {
        public DiagnosticsReport Diagnostics { get; }
        public LabelSymbolTable Labels { get; } = new();

        private LabelSymbolTableBuilder(DiagnosticsReport diagnostics)
        {
            throw new NotImplementedException("LabelSymbolTableBuilder not working with Label property in IStatement");
            Diagnostics = diagnostics;
        }

        private void AddLabel(IStatement stmt)
        {
            if (stmt.Label is not null && !Labels.AddLabeledStatement(stmt))
            {
                Diagnostics.AddError($"Label '{stmt.Label}' is already declared", stmt.Location);
            }
        }

        public static LabelSymbolTable Build(FuncDeclaration func, DiagnosticsReport diagnostics)
        {
            var visitor = new LabelSymbolTableBuilder(diagnostics);
            func.Accept(visitor, default);
            return visitor.Labels;
        }
    }
}
#endif

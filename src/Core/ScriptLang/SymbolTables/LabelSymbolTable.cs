namespace ScTools.ScriptLang.SymbolTables
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Statements;

    /// <summary>
    /// Table with all the labels available in a function or procedure.
    /// </summary>
    public sealed class LabelSymbolTable
    {
        private readonly Dictionary<string, IStatement> labels = new(ParserNew.CaseInsensitiveComparer);

        public LabelSymbolTable()
        {
        }

        public bool AddLabeledStatement(IStatement stmt)
        {
            Debug.Assert(stmt.Label is not null);
            return labels.TryAdd(stmt.Label.Name, stmt);
        }

        public IStatement? FindLabeledStatement(string name) => labels.TryGetValue(name, out var decl) ? decl : null;
    }
}

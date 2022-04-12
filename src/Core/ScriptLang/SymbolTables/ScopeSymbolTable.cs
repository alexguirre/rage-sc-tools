namespace ScTools.ScriptLang.SymbolTables
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Declarations;

    /// <summary>
    /// Table with scope support for local variables inside functions or procedures.
    /// The scopes are not persistant, once exited their contents are discarded.
    /// </summary>
    public sealed class ScopeSymbolTable
    {
        private readonly Stack<Dictionary<string, IValueDeclaration>> scopes = new();

        private Dictionary<string, IValueDeclaration> CurrentScope => scopes.Peek();

        public GlobalSymbolTable GlobalSymbols { get; }
        public bool HasScope => scopes.Count > 0;

        public ScopeSymbolTable(GlobalSymbolTable globalSymbols)
            => GlobalSymbols = globalSymbols;

        public void PushScope() => scopes.Push(new(ParserNew.CaseInsensitiveComparer));
        public void PopScope() => scopes.Pop();

        public bool AddValue(IValueDeclaration valueDeclaration) => CurrentScope.TryAdd(valueDeclaration.Name, valueDeclaration);

        public IValueDeclaration? FindValue(string name)
        {
            foreach (var scope in scopes)
            {
                if (scope.TryGetValue(name, out var decl))
                {
                    return decl;
                }
            }

            return GlobalSymbols.FindValue(name);
        }
    }
}

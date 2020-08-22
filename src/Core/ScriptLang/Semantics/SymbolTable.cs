#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class SymbolTable
    {
        private readonly Stack<ISymbol> symbols = new Stack<ISymbol>();
        private readonly List<(object Key, SymbolTable Table)> children = new List<(object, SymbolTable)>();


        public SymbolTable? Parent { get; }
        public IEnumerable<(object Key, SymbolTable Table)> Children => children;

        public IEnumerable<ISymbol> Symbols => symbols;

        public SymbolTable(SymbolTable? parent = null)
        {
            Parent = parent;
        }

        public void Add(ISymbol symbol)
        {
            if (LocalExists(symbol.Name))
            {
                throw new InvalidOperationException($"Symbol with name '{symbol.Name}' already exists in the current scope.");
            }

            symbols.Push(symbol);
        }

        public bool Exists(string name) => Lookup(name) != null;

        private bool LocalExists(string name) => LocalLookup(name) != null;

        /// <summary>
        /// Lookups a symbol in this scope and all its parent scopes.
        /// </summary>
        public ISymbol? Lookup(string name)
        {
            var table = this;
            while (table != null)
            {
                var symbol = table.LocalLookup(name);
                if (symbol != null)
                {
                    return symbol;
                }

                table = table.Parent;
            }

            return null;
        }

        /// <summary>
        /// Lookups a symbol only in this scope.
        /// </summary>
        private ISymbol? LocalLookup(string name)
        {
            foreach (var symbol in symbols)
            {
                if (symbol.Name == name)
                {
                    return symbol;
                }
            }

            return null;
        }

        /// <summary>
        /// Searches for a child scope with <paramref name="key"/> in this scope and all its children scope.
        /// </summary>
        public SymbolTable? FindScope(object key)
        {
            var (_, t) = Children.SingleOrDefault(c => c.Key == key);
            if (t != null)
            {
                return t;
            }

            foreach (var child in Children)
            {
                t = child.Table.FindScope(key);
                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new scope.
        /// </summary>
        public SymbolTable EnterScope(object key) 
        {
            if (Children.Any(c => c.Key == key))
            {
                throw new InvalidOperationException($"A scope with key '{key}' already exists");
            }

            var t = new SymbolTable(this);
            children.Add((key, t));
            return t;
        }

        /// <summary>
        /// Returns the parent scope. If this is the global scope, <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        public SymbolTable ExitScope()
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("Cannot exit global scope");
            }

            return Parent;
        }
    }

    public interface ISymbol
    {
        public string Name { get; }
        public SourceRange Source { get; }
    }
}

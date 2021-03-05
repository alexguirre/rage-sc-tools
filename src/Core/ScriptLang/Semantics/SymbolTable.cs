#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;

    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class SymbolTable
    {
        private readonly List<ISymbol> symbols = new(); // list to keep declaration order of the symbols
        private readonly Dictionary<string, ISymbol> symbolsDictionary = new(CaseInsensitiveComparer);
        private readonly List<SymbolTable> children = new();
        private readonly List<SymbolTable> imports = new();

        public Ast.Node AstNode { get; }
        public SymbolTable? Parent { get; }
        public IEnumerable<SymbolTable> Children => children;
        public IEnumerable<SymbolTable> Imports => imports;

        public bool IsGlobal => Parent == null;

        /// <summary>
        /// Returns symbols in the order they were declared in.
        /// </summary>
        public IEnumerable<ISymbol> Symbols => symbols;


        public SymbolTable(Ast.Root astNode)
        {
            AstNode = astNode;
            Parent = null;
        }

        public SymbolTable(Ast.StatementBlock astNode, SymbolTable parent)
        {
            AstNode = astNode;
            Parent = parent;
        }

        public void Import(SymbolTable symbols)
        {
            if (!symbols.IsGlobal)
            {
                throw new ArgumentException("Only global SymbolTables can be imported", nameof(symbols));
            }

            if (!IsGlobal)
            {
                throw new InvalidOperationException("Can only add import from a global SymbolTable");
            }

            imports.Add(symbols);
        }

        public void Add(ISymbol symbol)
        {
            if (LocalExists(symbol.Name))
            {
                throw new InvalidOperationException($"Symbol with name '{symbol.Name}' already exists in the current scope.");
            }

            symbols.Add(symbol);
            symbolsDictionary.Add(symbol.Name, symbol);
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
            ISymbol? symbol;
            if (symbolsDictionary.TryGetValue(name, out symbol))
            {
                return symbol;
            }

            if (IsGlobal)
            {
                if (BuiltIns.TryGetValue(name, out symbol))
                {
                    return symbol;
                }

                foreach (var import in imports)
                {
                    if (import.symbolsDictionary.TryGetValue(name, out symbol))
                    {
                        return symbol;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Searches for a child scope with <paramref name="key"/> in this scope and all its children scope.
        /// </summary>
        public SymbolTable? FindScope(Ast.StatementBlock node)
        {
            var t = Children.SingleOrDefault(c => c.AstNode == node);
            if (t != null)
            {
                return t;
            }

            foreach (var child in Children)
            {
                t = child.FindScope(node);
                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }

        public SymbolTable GetScope(Ast.StatementBlock node) => FindScope(node) ?? throw new ArgumentException("No scope for the specified AST node", nameof(node));

        /// <summary>
        /// Creates a new scope.
        /// </summary>
        public SymbolTable EnterScope(Ast.StatementBlock node) 
        {
            if (Children.Any(c => c.AstNode == node))
            {
                throw new InvalidOperationException($"A scope with AST node '{node}' already exists");
            }

            var t = new SymbolTable(node, this);
            children.Add(t);
            return t;
        }

        /// <summary>
        /// Returns the parent scope. If this is the global scope, <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        public SymbolTable ExitScope()
        {
            if (IsGlobal)
            {
                throw new InvalidOperationException("Cannot exit global scope");
            }
            Debug.Assert(Parent != null);

            return Parent;
        }

        public static readonly ImmutableDictionary<string, ISymbol> BuiltIns = CreateBuiltIns();

        private static ImmutableDictionary<string, ISymbol> CreateBuiltIns()
        {
            var dict = ImmutableDictionary.CreateBuilder<string, ISymbol>(CaseInsensitiveComparer);
            void Add(ISymbol symbol) => dict.Add(symbol.Name, symbol);

            // basic types
            Add(new TypeSymbol(nameof(BuiltInTypes.INT), SourceRange.Unknown, BuiltInTypes.INT));
            Add(new TypeSymbol(nameof(BuiltInTypes.FLOAT), SourceRange.Unknown, BuiltInTypes.FLOAT));
            Add(new TypeSymbol(nameof(BuiltInTypes.BOOL), SourceRange.Unknown, BuiltInTypes.BOOL));
            Add(new TypeSymbol(nameof(BuiltInTypes.STRING), SourceRange.Unknown, BuiltInTypes.STRING));

            // struct types
            foreach (var structTy in BuiltInTypes.STRUCTS)
            {
                Add(new TypeSymbol(structTy.Name!, SourceRange.Unknown, structTy));
            }

            foreach (var textLabelTy in BuiltInTypes.TEXT_LABELS)
            {
                Add(new TypeSymbol(textLabelTy.ToString(), SourceRange.Unknown, textLabelTy));
            }

            Add(new TypeSymbol(BuiltInTypes.ANY.ToString(), SourceRange.Unknown, BuiltInTypes.ANY));

            Add(IntrinsicFunctionSymbol.AssignString);
            Add(IntrinsicFunctionSymbol.AssignInt);
            Add(IntrinsicFunctionSymbol.AppendString);
            Add(IntrinsicFunctionSymbol.AppendInt);
            Add(IntrinsicFunctionSymbol.I2F);
            Add(IntrinsicFunctionSymbol.F2I);
            Add(IntrinsicFunctionSymbol.F2V);

            return dict.ToImmutable();
        }

        public static StringComparer CaseInsensitiveComparer => StringComparer.OrdinalIgnoreCase;
    }

    public interface ISymbol
    {
        string Name { get; }
        SourceRange Source { get; }
    }
}

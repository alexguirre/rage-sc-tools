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
        private readonly Stack<ISymbol> symbols = new();
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
        public IEnumerable<ISymbol> Symbols => symbols.Reverse();


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
            IEnumerable<ISymbol> availableSymbols = symbols;
            if (IsGlobal)
            {
                availableSymbols = availableSymbols.Concat(BuiltIns).Concat(imports.SelectMany(i => i.symbols));
            }

            foreach (var symbol in availableSymbols)
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

        public static readonly ImmutableArray<ISymbol> BuiltIns = CreateBuiltIns();

        private static ImmutableArray<ISymbol> CreateBuiltIns()
        {
            var arr = ImmutableArray.CreateBuilder<ISymbol>();

            // basic types
            var flTy = new BasicType(BasicTypeCode.Float);
            var intTy = new BasicType(BasicTypeCode.Int);
            arr.Add(new TypeSymbol("INT", SourceRange.Unknown, intTy));
            arr.Add(new TypeSymbol("FLOAT", SourceRange.Unknown, flTy));
            arr.Add(new TypeSymbol("BOOL", SourceRange.Unknown, new BasicType(BasicTypeCode.Bool)));
            arr.Add(new TypeSymbol("STRING", SourceRange.Unknown, new BasicType(BasicTypeCode.String)));

            // struct types
            static Field F(Type ty, string name) => new Field(ty, name);
            var structTypes = new[]
            {
                new StructType("VECTOR", F(flTy, "x"), F(flTy, "y"), F(flTy, "z")),
                new StructType("PLAYER_INDEX", F(intTy, "value")),
                new StructType("ENTITY_INDEX", F(intTy, "value")),
                new StructType("PED_INDEX", F(intTy, "value")),
                new StructType("VEHICLE_INDEX", F(intTy, "value")),
                new StructType("OBJECT_INDEX", F(intTy, "value")),
                new StructType("CAMERA_INDEX", F(intTy, "value")),
            };

            foreach (var structTy in structTypes)
            {
                arr.Add(new TypeSymbol(structTy.Name!, SourceRange.Unknown, structTy));
            }

            arr.Capacity = arr.Count;
            return arr.MoveToImmutable();
        }
    }

    public interface ISymbol
    {
        string Name { get; }
        SourceRange Source { get; }
    }
}

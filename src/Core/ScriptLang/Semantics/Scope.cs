#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using ScTools.ScriptLang.Semantics.Symbols;

    public sealed class Scope
    {
        private readonly List<Scope> children = new List<Scope>();
        private readonly Dictionary<string, ISymbol> symbols = new Dictionary<string, ISymbol>();

        public string DebugName { get; }
        public Scope? Parent { get; }
        public bool IsRoot => Parent == null;
        public IEnumerable<Scope> Children => children;

        private Scope(string debugName, Scope? parent)
            => (DebugName, Parent) = (debugName, parent);

        public bool Exists(string symbolName) => symbols.ContainsKey(symbolName) || (Parent?.Exists(symbolName) ?? false);

        public bool CanAdd(ISymbol symbol)
            => !Exists(symbol.Name) && symbol switch
            {
                PrimitiveSymbol _ => IsRoot,
                StructSymbol _ => IsRoot,
                StaticVariableSymbol _ => IsRoot,
                ProcedureSymbol _ => IsRoot,
                FunctionSymbol _ => IsRoot,
                ProcedurePrototypeSymbol _ => IsRoot,
                FunctionPrototypeSymbol _ => IsRoot,
                ParameterSymbol _ => !IsRoot,
                LocalSymbol _ => !IsRoot,
                StructFieldSymbol _ => !IsRoot,
                _ => throw new NotImplementedException()
            };

        public void Add(ISymbol symbol)
        {
            if (Exists(symbol.Name))
            {
                throw new InvalidOperationException($"Symbol with name '{symbol.Name}' already exists in this scope ({DebugName}).");
            }

            if (!CanAdd(symbol))
            {
                throw new InvalidOperationException($"Symbol '{symbol.Name}' of type {symbol.GetType().Name} cannot be added to the current scope ({DebugName}).");
            }

            symbols.Add(symbol.Name, symbol);
        }

        public bool TryFind(string symbolName, [NotNullWhen(true)] out ISymbol? symbol)
            => symbols.TryGetValue(symbolName, out symbol) || (Parent != null && Parent.TryFind(symbolName, out symbol));

        public Scope CreateNested(string debugName)
        {
            var s = new Scope(debugName, this);
            children.Add(s);
            return s;
        }

        public static Scope CreateRoot()
        {
            var s = new Scope("Root", null);
            s.Add(PrimitiveSymbol.Int);
            s.Add(PrimitiveSymbol.Float);
            s.Add(PrimitiveSymbol.Bool);
            s.Add(PrimitiveSymbol.Vec3);
            return s;
        }
    }
}

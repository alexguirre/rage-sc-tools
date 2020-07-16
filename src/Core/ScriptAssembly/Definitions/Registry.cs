namespace ScTools.ScriptAssembly.Definitions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ScTools.GameFiles;
    using ScTools.ScriptAssembly.Types;

    public sealed class Registry
    {
        public static uint NameToId(string name) => name.ToHash();

        private readonly Dictionary<uint, ISymbolDefinition> symbols = new Dictionary<uint, ISymbolDefinition>();
        private readonly List<FunctionDefinition> functions = new List<FunctionDefinition>();
        private readonly List<StaticFieldDefinition> staticFields = new List<StaticFieldDefinition>();
        private readonly List<ArgDefinition> args = new List<ArgDefinition>();

        public IEnumerable<FunctionDefinition> Functions => functions;
        public IEnumerable<StaticFieldDefinition> StaticFields => staticFields;
        public IEnumerable<ArgDefinition> Args => args;

        public TypeRegistry Types { get; } = new TypeRegistry();

        private void RegisterSymbol(ISymbolDefinition symbol)
        {
            if (!symbols.TryAdd(symbol.Id, symbol))
            {
                var existing = symbols[symbol.Id];
                string extraError = "";
                if (existing.Name != symbol.Name)
                {
                    extraError = $" (with name '{existing.Name}', hash collision?)";
                }
                throw new InvalidOperationException($"Symbol '{symbol.Name}' with ID {symbol.Id:X8} is already registered{extraError}");
            }

            if (symbol is FunctionDefinition func)
            {
                functions.Add(func);
            
                if (func.IsEntrypoint) // the entrypoint function needs to be the first in the code pages
                {
                    functions[^1] = functions[0];
                    functions[0] = func;
                }
            }
            else if (symbol is ArgDefinition arg)
            {
                args.Add(arg);
            }
            else if (symbol is StaticFieldDefinition sf)
            {
                staticFields.Add(sf);
            }
        }

        public ISymbolDefinition FindSymbol(string name) => FindSymbol(NameToId(name));
        public ISymbolDefinition FindSymbol(uint id) => symbols.TryGetValue(id, out var symbol) ? symbol : null;

        public StaticFieldDefinition FindStaticField(uint id) => FindSymbol(id) as StaticFieldDefinition;
        public StaticFieldDefinition FindStaticField(string name) => FindStaticField(NameToId(name));

        public FunctionDefinition FindFunction(uint id) => FindSymbol(id) as FunctionDefinition;
        public FunctionDefinition FindFunction(string name) => FindFunction(NameToId(name));

        public StaticFieldDefinition RegisterStaticField(string name, TypeBase type, ScriptValue initialValue)
        {
            var s = new StaticFieldDefinition(name, type, initialValue);
            RegisterSymbol(s);
            return s;
        }

        public ArgDefinition RegisterArg(string name, TypeBase type, ScriptValue initialValue)
        {
            var a = new ArgDefinition(name, type, initialValue);
            RegisterSymbol(a);
            return a;
        }

        public FunctionDefinition RegisterFunction(string name, bool naked, IEnumerable<FieldDefinition> args, IEnumerable<FieldDefinition> locals, TypeBase returnType, IEnumerable<FunctionDefinition.Statement> statements)
        {
            var f = new FunctionDefinition(name, naked, args, locals, returnType, statements);
            RegisterSymbol(f);
            return f;
        }
    }
}

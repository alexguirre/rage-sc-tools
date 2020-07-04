namespace ScTools.ScriptAssembly.Definitions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ScTools.GameFiles;

    public sealed class Registry
    {
        public static uint NameToId(string name) => name.ToHash();

        private readonly Dictionary<uint, ISymbolDefinition> symbols = new Dictionary<uint, ISymbolDefinition>();
        private readonly List<ArrayDefinition> arrayTypes = new List<ArrayDefinition>();
        private readonly List<FunctionDefinition> functions = new List<FunctionDefinition>();

        public IEnumerable<FunctionDefinition> Functions => functions;

        public Registry()
        {
            RegisterSymbol(AutoTypeDefintion.Instance);
        }

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

            if (symbol is ArrayDefinition arr)
            {
                arrayTypes.Add(arr);
            }
            else if (symbol is FunctionDefinition func)
            {
                functions.Add(func);
            }
        }

        public ISymbolDefinition FindSymbol(string name) => FindSymbol(NameToId(name));
        public ISymbolDefinition FindSymbol(uint id) => symbols.TryGetValue(id, out var symbol) ? symbol : null;

        public TypeDefinition FindType(uint id) => FindSymbol(id) as TypeDefinition;
        public TypeDefinition FindType(string name) => FindType(NameToId(name));

        public StaticFieldDefinition FindStaticField(uint id) => FindSymbol(id) as StaticFieldDefinition;
        public StaticFieldDefinition FindStaticField(string name) => FindStaticField(NameToId(name));

        public FunctionDefinition FindFunction(uint id) => FindSymbol(id) as FunctionDefinition;
        public FunctionDefinition FindFunction(string name) => FindFunction(NameToId(name));

        public ArrayDefinition FindArray(TypeDefinition itemType, uint length)
        {
            for (int i = 0; i < arrayTypes.Count; i++)
            {
                var a = arrayTypes[i];
                if (a.ItemType == itemType && a.Length == length)
                {
                    return a;
                }
            }

            return null;
        }

        public StructDefinition RegisterStruct(string name, IEnumerable<FieldDefinition> fields)
        {
            var s = new StructDefinition(name, fields);
            RegisterSymbol(s);
            return s;
        }

        public ArrayDefinition RegisterArray(TypeDefinition itemType, uint length)
        {
            var a = new ArrayDefinition(itemType, length);
            RegisterSymbol(a);
            return a;
        }

        /// <summary>
        /// Finds a registered array type, and registers a new array type if not found.
        /// If a type with the specified name does not exist, returns <c>null</c>.
        /// </summary>
        public ArrayDefinition FindOrRegisterArray(string name, uint length)
        {
            var t = FindType(name);
            if (t == null)
            {
                return null;
            }

            return FindArray(t, length) ?? RegisterArray(t, length);
        }

        public StaticFieldDefinition RegisterStaticField(string name, TypeDefinition type, ScriptValue initialValue)
        {
            var s = new StaticFieldDefinition(name, type, initialValue);
            RegisterSymbol(s);
            return s;
        }

        public FunctionDefinition RegisterFunction(string name, bool naked, IEnumerable<FieldDefinition> args, IEnumerable<FieldDefinition> locals, TypeDefinition returnType, IEnumerable<FunctionDefinition.Statement> statements)
        {
            var f = new FunctionDefinition(name, naked, args, locals, returnType, statements);
            RegisterSymbol(f);
            return f;
        }
    }
}

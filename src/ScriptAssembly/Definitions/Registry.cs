namespace ScTools.ScriptAssembly.Definitions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class Registry
    {
        public static uint NameToId(string name) => name.ToHash();

        private readonly List<TypeDefinition> types = new List<TypeDefinition>();
        private readonly List<StaticFieldDefinition> staticFields = new List<StaticFieldDefinition>();
        private readonly List<FunctionDefinition> functions = new List<FunctionDefinition>();

        public Registry()
        {
            RegisterType(AutoTypeDefintion.Instance);
        }

        private void RegisterType(TypeDefinition type)
        {
            if (types.Exists(t => t.Id == type.Id))
            {
                var existingType = types.First(t => t.Id == type.Id);
                string extraError = "";
                if (existingType.Name != type.Name)
                {
                    extraError = $" (with name '{existingType.Name}', hash collision?)";
                }
                throw new InvalidOperationException($"Type '{type.Name}' with ID {type.Id:X8} is already registered{extraError}");
            }

            types.Add(type);
        }

        public TypeDefinition FindType(uint id)
        {
            for (int i = 0; i < types.Count; i++)
            {
                if (types[i].Id == id)
                {
                    return types[i];
                }
            }

            return null;
        }

        public TypeDefinition FindType(string name) => FindType(NameToId(name));

        public ArrayDefinition FindArray(TypeDefinition itemType, uint length)
        {
            for (int i = 0; i < types.Count; i++)
            {
                if (types[i] is ArrayDefinition a && a.ItemType == itemType && a.Length == length)
                {
                    return a;
                }
            }

            return null;
        }

        public StructDefinition RegisterStruct(string name, IEnumerable<FieldDefinition> fields)
        {
            var s = new StructDefinition(name, fields);
            RegisterType(s);
            return s;
        }

        public ArrayDefinition RegisterArray(TypeDefinition itemType, uint length)
        {
            var a = new ArrayDefinition(itemType, length);
            RegisterType(a);
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


        private void RegisterStaticField(StaticFieldDefinition staticField)
        {
            if (staticFields.Exists(t => t.Id == staticField.Id))
            {
                var existingField = types.First(t => t.Id == staticField.Id);
                string extraError = "";
                if (existingField.Name != staticField.Name)
                {
                    extraError = $" (with name '{existingField.Name}', hash collision?)";
                }
                throw new InvalidOperationException($"Static field '{staticField.Name}' with ID {staticField.Id:X8} is already registered{extraError}");
            }

            staticFields.Add(staticField);
        }

        public StaticFieldDefinition RegisterStaticField(string name, TypeDefinition type)
        {
            var s = new StaticFieldDefinition(name, type);
            RegisterStaticField(s);
            return s;
        }


        private void RegisterFunction(FunctionDefinition function)
        {
            if (functions.Exists(t => t.Id == function.Id))
            {
                var existingFunc = types.First(t => t.Id == function.Id);
                string extraError = "";
                if (existingFunc.Name != function.Name)
                {
                    extraError = $" (with name '{existingFunc.Name}', hash collision?)";
                }
                throw new InvalidOperationException($"Function '{function.Name}' with ID {function.Id:X8} is already registered{extraError}");
            }

            functions.Add(function);
        }

        public FunctionDefinition RegisterFunction(string name, bool naked, IEnumerable<FieldDefinition> args, IEnumerable<FieldDefinition> locals, IEnumerable<FunctionDefinition.Statement> statements)
        {
            var f = new FunctionDefinition(name, naked, args, locals, statements);
            RegisterFunction(f);
            return f;
        }
    }
}

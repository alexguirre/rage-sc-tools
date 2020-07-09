namespace ScTools.ScriptAssembly.Types
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ScTools.GameFiles;

    public sealed class TypeRegistry
    {
        public static uint NameToId(string name) => name.ToHash();

        private readonly Dictionary<uint, TypeBase> types = new Dictionary<uint, TypeBase>();
        private readonly List<ArrayType> arrays = new List<ArrayType>();
        private readonly List<StructType> structs = new List<StructType>();

        public IEnumerable<StructType> Structs => structs;

        public TypeRegistry()
        {
            Register(AutoType.Instance);
        }

        private void Register(TypeBase type)
        {
            if (!types.TryAdd(type.Id, type))
            {
                var existing = types[type.Id];
                string extraError = "";
                if (existing.Name != type.Name)
                {
                    extraError = $" (with name '{existing.Name}', hash collision?)";
                }
                throw new InvalidOperationException($"Symbol '{type.Name}' with ID {type.Id:X8} is already registered{extraError}");
            }

            if (type is ArrayType arr)
            {
                arrays.Add(arr);
            }
            else if (type is StructType s)
            {
                structs.Add(s);
            }
        }

        public void Unregister(uint id, bool unregisterDependantTypes)
        {
            if (!types.TryGetValue(id, out var type))
            {
                throw new ArgumentException($"Type with ID {id:X8} is not registered");
            }

            if (id == AutoType.Instance.Id)
            {
                throw new InvalidOperationException("Type AUTO cannot be unregistered");
            }

            foreach (var a in arrays)
            {
                if (a.ItemType.Id == id)
                {
                    if (unregisterDependantTypes)
                    {
                        Unregister(id, true);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Cannot unregister type {type.Name} (ID: {id:X8}): array type {a.Name} (ID: {a.Id:X8}) depends on it");
                    }
                }
            }

            foreach (var s in structs)
            {
                if (s.Fields.Any(f => f.Type.Id == id))
                {
                    if (unregisterDependantTypes)
                    {
                        Unregister(id, true);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Cannot unregister type {type.Name} (ID: {id:X8}): struct type {s.Name} (ID: {s.Id:X8}) depends on it");
                    }
                }
            }

            types.Remove(id);
            if (type is ArrayType arr)
            {
                arrays.Remove(arr);
            }
            else if (type is StructType st)
            {
                structs.Remove(st);
            }
        }

        public TypeBase FindType(uint id) => types.TryGetValue(id, out var t) ? t : null;
        public TypeBase FindType(string name) => FindType(NameToId(name));

        public ArrayType FindArray(TypeBase itemType, uint length)
        {
            for (int i = 0; i < arrays.Count; i++)
            {
                var a = arrays[i];
                if (a.ItemType == itemType && a.Length == length)
                {
                    return a;
                }
            }

            return null;
        }

        public StructType RegisterStruct(string name, IEnumerable<StructField> fields)
        {
            var s = new StructType(name, fields);
            Register(s);
            return s;
        }

        public ArrayType RegisterArray(TypeBase itemType, uint length)
        {
            var a = new ArrayType(itemType, length);
            Register(a);
            return a;
        }

        /// <summary>
        /// Finds a registered array type, and registers a new array type if not found.
        /// If a type with the specified name does not exist, returns <c>null</c>.
        /// </summary>
        public ArrayType FindOrRegisterArray(string name, uint length)
            => FindOrRegisterArray(FindType(name), length);

        public ArrayType FindOrRegisterArray(TypeBase itemType, uint length)
        {
            if (itemType == null)
            {
                return null;
            }

            return FindArray(itemType, length) ?? RegisterArray(itemType, length);
        }
    }
}

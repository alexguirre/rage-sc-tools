namespace ScTools.ScriptAssembly.Types
{
    using System;

    public abstract class TypeBase
    {
        public uint Id { get; }
        public string Name { get; }
        public abstract uint SizeOf { get; }

        public TypeBase(string name)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("null or empty string", nameof(name));
            Id = TypeRegistry.NameToId(name);
        }
    }
}

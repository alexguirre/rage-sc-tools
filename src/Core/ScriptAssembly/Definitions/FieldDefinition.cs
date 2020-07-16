namespace ScTools.ScriptAssembly.Definitions
{
    using System;
    using ScTools.ScriptAssembly.Types;

    public class FieldDefinition
    {
        public string Name { get; }
        public TypeBase Type { get; }

        public FieldDefinition(string name, TypeBase type)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("null or empty string", nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}

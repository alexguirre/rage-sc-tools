namespace ScTools.ScriptAssembly.Definitions
{
    using System;

    public class FieldDefinition
    {
        public string Name { get; }
        public TypeDefinition Type { get; }

        public FieldDefinition(string name, TypeDefinition type)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("null or empty string", nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}

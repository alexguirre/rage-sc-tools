namespace ScTools.ScriptAssembly.Definitions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public abstract class TypeDefinition : ISymbolDefinition
    {
        public uint Id { get; }
        public string Name { get; }
        public abstract uint SizeOf { get; }

        public TypeDefinition(string name)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("null or empty string", nameof(name));
            Id = Registry.NameToId(name);
        }
    }

    public sealed class AutoTypeDefintion : TypeDefinition
    {
        public override uint SizeOf => 1;

        private AutoTypeDefintion() : base("AUTO")
        {
        }

        public static AutoTypeDefintion Instance { get; } = new AutoTypeDefintion();
    }

    public sealed class ArrayDefinition : TypeDefinition
    {
        public override uint SizeOf => 1 + ItemType.SizeOf * Length;

        public TypeDefinition ItemType { get; }
        public uint Length { get; }

        public ArrayDefinition(TypeDefinition itemType, uint length) : base($"{itemType.Name}[{length}]")
        {
            ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
            Length = length > 0 ? length : throw new ArgumentOutOfRangeException(nameof(length), "length is 0");
        }
    }

    public sealed class StructDefinition : TypeDefinition
    {
        public override uint SizeOf => (uint)Fields.Sum(f => f.Type.SizeOf);

        public ImmutableArray<FieldDefinition> Fields { get; }

        public StructDefinition(string name, IEnumerable<FieldDefinition> fields) : base(name)
        {
            Fields = fields?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(fields));
        }
    }
}

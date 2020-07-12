namespace ScTools.ScriptAssembly.Types
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public sealed class StructType : TypeBase
    {
        public override uint SizeOf => (uint)Fields.Sum(f => f.Type.SizeOf);

        public ImmutableArray<StructField> Fields { get; }
        public ImmutableArray<uint> Offsets { get; }

        public StructType(string name, IEnumerable<StructField> fields) : base(name)
        {
            Fields = fields?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(fields));

            var offsets = ImmutableArray.CreateBuilder<uint>(Fields.Length);

            uint offset = 0;
            for (int i = 0; i < Fields.Length; i++)
            {
                offsets.Add(offset);
                offset += Fields[i].Type.SizeOf;
            }

            Offsets = offsets.MoveToImmutable();
        }

        public int IndexOfField(string fieldName)
        {
            int i = 0;
            foreach (var f in Fields)
            {
                if (f.Name == fieldName)
                {
                    return i;
                }

                i++;
            }

            return -1;
        }
    }

    public readonly struct StructField
    {
        public string Name { get; }
        public TypeBase Type { get; }

        public StructField(string name, TypeBase type)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("null or empty string", nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}

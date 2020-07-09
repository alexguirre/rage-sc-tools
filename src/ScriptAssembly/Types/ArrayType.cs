namespace ScTools.ScriptAssembly.Types
{
    using System;

    public sealed class ArrayType : TypeBase
    {
        public override uint SizeOf => 1 + ItemType.SizeOf * Length;

        public TypeBase ItemType { get; }
        public uint Length { get; }

        public ArrayType(TypeBase itemType, uint length) : base($"{itemType.Name}[{length}]")
        {
            ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
            Length = length > 0 ? length : throw new ArgumentOutOfRangeException(nameof(length), "length is 0");
        }
    }
}

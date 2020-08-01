#nullable enable
using ScTools.ScriptLang.Semantics.Symbols;

namespace ScTools.ScriptLang.Semantics
{
    public readonly struct TypeInfo
    {
        public string Name { get; }
        public bool IsReference { get; }
        public int ArraySize { get; }

        public bool IsArray => ArraySize > 0;

        public TypeInfo(string name, bool isReference, int arraySize)
            => (Name, IsReference, ArraySize) = (name, isReference, arraySize);

        public static TypeInfo Int => Create(PrimitiveSymbol.Int.Name);
        public static TypeInfo Float => Create(PrimitiveSymbol.Float.Name);
        public static TypeInfo Bool => Create(PrimitiveSymbol.Bool.Name);
        public static TypeInfo Vec3 => Create(PrimitiveSymbol.Vec3.Name);
        public static TypeInfo Create(string typeName) => new TypeInfo(typeName, false, 0);
        public static TypeInfo CreateRef(string typeName) => new TypeInfo(typeName, true, 0);
        public static TypeInfo CreateArray(string itemTypeName, int arraySize) => new TypeInfo(itemTypeName, false, arraySize);
        public static TypeInfo CreateArrayOfRefs(string itemTypeName, int arraySize) => new TypeInfo(itemTypeName, true, arraySize);
    }
}

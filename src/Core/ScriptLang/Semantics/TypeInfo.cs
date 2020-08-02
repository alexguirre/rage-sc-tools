#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;

    using ScTools.ScriptLang.Semantics.Symbols;

    using TC = TypeCapabilities;

    public readonly struct TypeInfo
    {
        public string Name { get; }
        public bool IsReference { get; }
        public int ArraySize { get; }

        public bool IsArray => ArraySize > 0;

        public TC Capabilities => capabilities.TryGetValue(Name, out var cap) ? cap : TC.None;

        public TypeInfo(string name, bool isReference, int arraySize)
            => (Name, IsReference, ArraySize) = (name, isReference, arraySize);

        public TypeInfo Deref()
        {
            Debug.Assert(IsReference);
            Debug.Assert(!IsArray);

            return Create(Name);
        }

        public TypeInfo ItemType()
        {
            Debug.Assert(IsArray);

            return IsReference ? CreateRef(Name) : Create(Name);
        }

        public override string ToString()
            => $"{Name}{(IsReference ? "&" : "")}{(IsArray ? $"[{ArraySize}]" : "")}";

        public static TypeInfo Int => Create(PrimitiveSymbol.Int.Name);
        public static TypeInfo Float => Create(PrimitiveSymbol.Float.Name);
        public static TypeInfo Bool => Create(PrimitiveSymbol.Bool.Name);
        public static TypeInfo Vec3 => Create(PrimitiveSymbol.Vec3.Name);
        public static TypeInfo Create(string typeName) => new TypeInfo(typeName, false, 0);
        public static TypeInfo CreateRef(string typeName) => new TypeInfo(typeName, true, 0);
        public static TypeInfo CreateArray(string itemTypeName, int arraySize) => new TypeInfo(itemTypeName, false, arraySize);
        public static TypeInfo CreateArrayOfRefs(string itemTypeName, int arraySize) => new TypeInfo(itemTypeName, true, arraySize);

        private static readonly ImmutableDictionary<string, TC> capabilities = new Dictionary<string, TC>
        {
            { Int.Name,     TC.Add | TC.Subtract | TC.Multiply | TC.Divide | TC.Modulo | TC.BitwiseOr | TC.BitwiseAnd | TC.BitwiseXor | TC.Negate | TC.Not },
            { Float.Name,   TC.Add | TC.Subtract | TC.Multiply | TC.Divide | TC.Modulo | TC.Negate },
            { Bool.Name,    TC.Not },
            { Vec3.Name,    TC.Add | TC.Subtract | TC.Multiply | TC.Divide | TC.Negate },
        }.ToImmutableDictionary();
    }

    [Flags]
    public enum TypeCapabilities
    {
        None = 0,
        Add = 1 << 0,
        Subtract = 1 << 1,
        Multiply = 1 << 2,
        Divide = 1 << 3,
        Modulo = 1 << 4,
        BitwiseOr = 1 << 5,
        BitwiseAnd = 1 << 6,
        BitwiseXor = 1 << 7,
        Negate = 1 << 8,
        Not = 1 << 9,
    }
}

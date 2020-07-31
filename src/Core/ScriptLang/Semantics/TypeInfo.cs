#nullable enable
namespace ScTools.ScriptLang.Semantics
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using ScTools.ScriptLang.Semantics.Symbols;

    public abstract class TypeInfo
    {
        public static BasicTypeInfo CreateInt() => new BasicTypeInfo(PrimitiveSymbol.Int.Name);
        public static BasicTypeInfo CreateFloat() => new BasicTypeInfo(PrimitiveSymbol.Float.Name);
        public static BasicTypeInfo CreateBool() => new BasicTypeInfo(PrimitiveSymbol.Bool.Name);
        public static BasicTypeInfo Create(string name) => new BasicTypeInfo(name);
        public static RefTypeInfo CreateRef(string name) => new RefTypeInfo(name);
        public static ProcedureRefTypeInfo CreateProcedureRef(IEnumerable<TypeInfo> parameters) => new ProcedureRefTypeInfo(parameters);
        public static FunctionRefTypeInfo CreateFunctionRef(TypeInfo returnType, IEnumerable<TypeInfo> parameters) => new FunctionRefTypeInfo(returnType, parameters);
        public static ArrayTypeInfo CreateArray(TypeInfo itemType, int arraySize) => new ArrayTypeInfo(itemType, arraySize);
        public static AggregateTypeInfo CreateAggregate(IEnumerable<TypeInfo> types) => new AggregateTypeInfo(types);
    }

    public sealed class BasicTypeInfo : TypeInfo
    {
        public string Name { get; }

        public BasicTypeInfo(string name) => Name = name;
    }

    public sealed class RefTypeInfo : TypeInfo
    {
        public string Name { get; }

        public RefTypeInfo(string name) => Name = name;
    }

    public sealed class ProcedureRefTypeInfo : TypeInfo
    {
        public ImmutableArray<TypeInfo> Parameters { get; }

        public ProcedureRefTypeInfo(IEnumerable<TypeInfo> parameters)
            => Parameters = parameters.ToImmutableArray();
    }

    public sealed class FunctionRefTypeInfo : TypeInfo
    {
        public TypeInfo ReturnType { get; }
        public ImmutableArray<TypeInfo> Parameters { get; }

        public FunctionRefTypeInfo(TypeInfo returnType, IEnumerable<TypeInfo> parameters)
            => (ReturnType, Parameters) = (returnType, parameters.ToImmutableArray());
    }

    public sealed class ArrayTypeInfo : TypeInfo
    {
        public TypeInfo ItemType { get; }
        public int ArraySize { get; }

        public ArrayTypeInfo(TypeInfo itemType, int arraySize)
        {
            if (arraySize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arraySize), "Must be greater than 0");
            }

            ItemType = itemType;
            ArraySize = arraySize;
        }
    }

    public sealed class AggregateTypeInfo : TypeInfo
    {
        public ImmutableArray<TypeInfo> Types { get; }

        public AggregateTypeInfo(IEnumerable<TypeInfo> types)
            => Types = types.ToImmutableArray();
    }
}

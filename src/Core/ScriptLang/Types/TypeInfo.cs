namespace ScTools.ScriptLang.Types;

using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Semantics;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

[DebuggerDisplay("{ToPrettyString(),nq}")]
public abstract record TypeInfo
{
    public abstract int SizeOf { get; }
    public abstract ImmutableArray<FieldInfo> Fields { get; }
    public virtual bool IsError => false;

    public abstract string ToPrettyString();
    public abstract TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor);
}

public sealed record FieldInfo(TypeInfo Type, string Name, int Offset);

public sealed record ErrorType : TypeInfo
{
    public override int SizeOf => 0;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;
    public override bool IsError => true;

    public override string ToPrettyString() => "<type error>";
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);

    public static ErrorType Instance { get; } = new();
}

public sealed record VoidType : TypeInfo
{
    public override int SizeOf => 0;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override string ToPrettyString() => "<void>";
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);

    public static VoidType Instance { get; } = new();
}

public abstract record PrimitiveType<TSelf> : TypeInfo where TSelf : new()
{
    public override int SizeOf => 1;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public static TSelf Instance { get; } = new();
}

public sealed record IntType : PrimitiveType<IntType>
{
    public override string ToPrettyString() => "INT";
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}
public sealed record FloatType : PrimitiveType<FloatType>
{
    public override string ToPrettyString() => "FLOAT";
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}
public sealed record BoolType : PrimitiveType<BoolType>
{
    public override string ToPrettyString() => "BOOL";
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}
public sealed record StringType : PrimitiveType<StringType>
{
    public override string ToPrettyString() => "STRING";
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}
public sealed record NullType : PrimitiveType<NullType>
{
    public override string ToPrettyString() => "NULL";
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}
public sealed record AnyType : PrimitiveType<AnyType>
{
    public override string ToPrettyString() => "ANY";
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

public sealed record VectorType : TypeInfo
{
    public override int SizeOf => 3;
    public override ImmutableArray<FieldInfo> Fields => fields;

    public override string ToPrettyString() => "VECTOR";
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);

    public static VectorType Instance { get; } = new();

    private static readonly ImmutableArray<FieldInfo> fields = ImmutableArray.Create(
        new FieldInfo(FloatType.Instance, "x", 0),
        new FieldInfo(FloatType.Instance, "y", 1),
        new FieldInfo(FloatType.Instance, "z", 2));
}

/// <summary>
/// Strongly typed integer that represents a handle to a native object.
/// </summary>
/// <param name="Declaration">Declaration of this native type.</param>
/// <param name="Base">Base type of this native type. This makes this native type implicitly convertible to the base native type. For example PED_INDEX/VEHICLE_INDEX/OBJECT_INDEX to ENTITY_INDEX.</param>
public sealed record NativeType(NativeTypeDeclaration Declaration, NativeType? Base) : TypeInfo
{
    public override int SizeOf => 1;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override string ToPrettyString() => Declaration.Name;
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

public sealed record EnumType(EnumDeclaration Declaration) : TypeInfo
{
    public override int SizeOf { get; } = 1;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;
    public bool IsStrict => Declaration.IsStrict;
    public bool IsHash => Declaration.IsHash;

    public override string ToPrettyString() => Declaration.Name;
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

public record StructType(StructDeclaration Declaration, ImmutableArray<FieldInfo> Fields) : TypeInfo
{
    public override int SizeOf { get; } = Fields.Sum(f => f.Type.SizeOf);
    private ImmutableArrayWithSequenceEquality<FieldInfo> FieldsBacking { get; } = new(Fields);
    public override ImmutableArray<FieldInfo> Fields => FieldsBacking.Array;

    public override string ToPrettyString() => Declaration.Name;
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

public record FunctionType(TypeInfo Return, ImmutableArray<ParameterInfo> Parameters) : TypeInfo
{
    public override int SizeOf => 1;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;
    private ImmutableArrayWithSequenceEquality<ParameterInfo> ParametersBacking { get; } = new(Parameters);
    public ImmutableArray<ParameterInfo> Parameters => ParametersBacking.Array;

    public override string ToPrettyString()
        => $"{(Return is VoidType ? "PROC" : $"FUNC {Return.ToPrettyString()}")}({string.Join(", ", Parameters.Select(p => p.ToPrettyString()))})";
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

public sealed record ParameterInfo(TypeInfo Type, bool IsReference, IExpression? OptionalInitializer = null)
{
    public bool IsReference { get; } = IsReference || Type is ArrayType;
    public bool IsOptional => OptionalInitializer is not null;
    public int SizeOf => IsReference ? 1 : Type.SizeOf;
    public string ToPrettyString() => Type.ToPrettyString() + (IsReference && Type is not ArrayType ? "&" : "");
}

public sealed record ArrayType(TypeInfo Item, int Length) : TypeInfo
{
    public override int SizeOf { get; } = 1 + Item.SizeOf * Length;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override string ToPrettyString()
    {
        var sb = new System.Text.StringBuilder();
        TypeInfo type = this;
        while (type is ArrayType arrayType)
        {
            sb.Append('[');
            sb.Append(arrayType.Length);
            sb.Append(']');
            type = arrayType.Item;
        }

        return type.ToPrettyString() + sb.ToString();
    }
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

/// <summary>
/// Represents a type name when used as a expression.
/// For example, in INT_TO_ENUM(MYENUM, 1) the type of 'MYENUM' would be <see cref="TypeNameType"/>.
/// </summary>
public sealed record TypeNameType(ITypeSymbol TypeSymbol) : TypeInfo
{
    public override int SizeOf => 0;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override string ToPrettyString() => TypeSymbol.Name;
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

/// <summary>
/// Wrapper for <see cref="ImmutableArray{T}"/> that implements value equality instead of reference equality.
/// Different arrays with equal items are considered equal
/// </summary>
/// <typeparam name="T"></typeparam>
internal readonly struct ImmutableArrayWithSequenceEquality<T> : IEquatable<ImmutableArrayWithSequenceEquality<T>>
{
    public readonly ImmutableArray<T> Array;

    public ImmutableArrayWithSequenceEquality(ImmutableArray<T> array) => Array = array;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is ImmutableArrayWithSequenceEquality<T> other && Equals(other);

    public bool Equals(ImmutableArrayWithSequenceEquality<T> other)
        => Array.SequenceEqual(other.Array);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        Array.ForEach(i => hc.Add(i));
        return hc.ToHashCode();
    }
}

namespace ScTools.ScriptLang.Types;

using ScTools.ScriptLang.Ast.Declarations;

using System.Collections.Immutable;
using System.Linq;

public abstract record TypeInfo
{
    public abstract int SizeOf { get; }
    public abstract ImmutableArray<FieldInfo> Fields { get; }

    public abstract TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor);
}

public sealed record FieldInfo(TypeInfo Type, string Name);

public sealed record ErrorType : TypeInfo
{
    public override int SizeOf => 0;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);

    public static ErrorType Instance { get; } = new();
}

public sealed record VoidType : TypeInfo
{
    public override int SizeOf => 0;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

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
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}
public sealed record FloatType : PrimitiveType<FloatType>
{
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}
public sealed record BoolType : PrimitiveType<BoolType>
{
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}
public sealed record StringType : PrimitiveType<StringType>
{
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}
public sealed record NullType : PrimitiveType<NullType>
{
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}
public sealed record AnyType : PrimitiveType<AnyType>
{
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

public sealed record VectorType : TypeInfo
{
    public override int SizeOf => 3;
    public override ImmutableArray<FieldInfo> Fields { get; } = ImmutableArray.Create(
        new FieldInfo(FloatType.Instance, "x"),
        new FieldInfo(FloatType.Instance, "y"),
        new FieldInfo(FloatType.Instance, "z"));

    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);

    public static VectorType Instance { get; } = new();
}

public sealed record EnumType(string Name) : TypeInfo
{
    public override int SizeOf { get; } = 1;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

public sealed record StructType(string Name, ImmutableArray<FieldInfo> Fields) : TypeInfo
{
    public override int SizeOf { get; } = Fields.Sum(f => f.Type.SizeOf);
    public override ImmutableArray<FieldInfo> Fields { get; } = Fields;

    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

public sealed record FunctionType(TypeInfo Return, ImmutableArray<TypeInfo> Parameters) : TypeInfo
{
    public override int SizeOf => 1;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

public sealed record RefType(TypeInfo Pointee) : TypeInfo
{
    public override int SizeOf => 1;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

public sealed record ArrayType(TypeInfo Item, int Length) : TypeInfo
{
    public override int SizeOf { get; } = 1 + Item.SizeOf * Length;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

/// <summary>
/// Represents a type name when used as a expression.
/// For example, in INT_TO_ENUM(MYENUM, 1) the type of 'MYENUM' would be <see cref="TypeNameType"/>.
/// </summary>
public sealed record TypeNameType(ITypeDeclaration TypeDeclaration) : TypeInfo
{
    public override int SizeOf => 0;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

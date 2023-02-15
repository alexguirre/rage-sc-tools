namespace ScTools.ScriptLang.Types;

using ScTools.ScriptLang.Ast.Declarations;

using System;
using System.Collections.Immutable;

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

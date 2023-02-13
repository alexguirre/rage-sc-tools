namespace ScTools.ScriptLang.Types;

using ScTools.ScriptLang.Ast.Declarations;

using System;
using System.Collections.Immutable;

/// <summary>
/// Strongly typed integer that represents a handle to a native object.
/// </summary>
/// <param name="Kind">Native object type identifier.</param>
public sealed record NativeType(NativeTypeDeclaration Declaration) : TypeInfo
{
    public override int SizeOf => 1;
    public override ImmutableArray<FieldInfo> Fields => ImmutableArray<FieldInfo>.Empty;

    public override string ToPrettyString() => Declaration.Name;
    public override TReturn Accept<TReturn>(ITypeVisitor<TReturn> visitor) => visitor.Visit(this);
}

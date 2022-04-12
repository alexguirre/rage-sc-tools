namespace ScTools.ScriptLang.Ast.Types;

using System;

/// <summary>
/// Represents a type with a name that can be resolved. Once resolved, it behaves like a thin wrapper around the resolved type.
/// </summary>
public sealed class NamedType : BaseType
{
    public string Name { get; set; }
    public IType? ResolvedType { get; set; }
    /// <summary>
    /// Gets <see cref="ResolvedType"/>, but if it was not resolved yet (i.e. it is <c>null</c>) throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public IType CheckedResolvedType => ResolvedType ?? throw new InvalidOperationException("");
    public override int SizeOf => CheckedResolvedType.SizeOf;

    public NamedType(Token nameIdentifier) : base(nameIdentifier)
        => Name = nameIdentifier.Lexeme.ToString();
    public NamedType(SourceRange source, string name) : base(source)
        => Name = name;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override bool Equivalent(IType other) => other is NamedType otherNamed && ParserNew.CaseInsensitiveComparer.Equals(Name, otherNamed.Name);
}

namespace ScTools.ScriptLang.Ast.Types;

using ScTools.ScriptLang.Ast.Errors;

using System.Diagnostics;

/// <summary>
/// Represents an array without specific size.
/// Always a reference and only allowed as a function parameter.
/// </summary>
public sealed class IncompleteArrayType : BaseArrayType
{
    public override int SizeOf => 0; // NOTE: incomplete arrays are always references, cannot get their size

    public IncompleteArrayType(Token openBracket, Token closeBracket, IType itemType) : base(itemType, openBracket, closeBracket)
    {
        Debug.Assert(openBracket.Kind is TokenKind.OpenBracket);
        Debug.Assert(closeBracket.Kind is TokenKind.CloseBracket);
    }
    public IncompleteArrayType(SourceRange source, IType itemType) : base(source, itemType) { }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override bool Equivalent(IType other)
        => other is IncompleteArrayType otherArray && ItemType.Equivalent(otherArray.ItemType);

    // incomplete array types can reference arrays of any size if their item types are equivalent
    public override bool CanBindRefTo(IType other)
        => other is IArrayType otherArray && ItemType.Equivalent(otherArray.ItemType);

    public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs is ErrorType;
}

namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;

    /// <summary>
    /// Represents an array without specific size.
    /// Only allowed as function parameter wrapped in <see cref="RefType"/>.
    /// </summary>
    public sealed class IncompleteArrayType : BaseArrayType
    {
        public override int SizeOf => 0;

        public IncompleteArrayType(SourceRange source, IType itemType) : base(source, itemType) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other)
            => other is IncompleteArrayType otherArray && ItemType.Equivalent(otherArray.ItemType);

        // incomplete array types can reference arrays of any size if their item types are equivalent
        public override bool CanBindRefTo(IType other)
            => other is IArrayType otherArray && ItemType.Equivalent(otherArray.ItemType);

        public override bool CanAssign(IType rhs) => rhs is ErrorType;
    }
}

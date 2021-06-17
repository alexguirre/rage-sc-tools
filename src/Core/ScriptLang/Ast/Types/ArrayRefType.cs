namespace ScTools.ScriptLang.Ast.Types
{
    /// <summary>
    /// Represents a reference to an array of any size.
    /// </summary>
    public sealed class ArrayRefType : BaseType
    {
        public IType ItemType { get; set; }

        public override int SizeOf => 1;

        public ArrayRefType(SourceRange source, IType itemType) : base(source)
            => ItemType = itemType;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

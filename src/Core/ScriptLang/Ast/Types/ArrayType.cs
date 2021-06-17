namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Expressions;

    /// <summary>
    /// Represents an array type of constant size.
    /// </summary>
    public sealed class ArrayType : BaseType
    {
        public IType ItemType { get; set; }
        public int Length { get; set; }
        public IExpression LengthExpression { get; set; }

        public override int SizeOf => 1 + ItemType.SizeOf * Length;

        public ArrayType(SourceRange source, IType itemType, IExpression lengthExpr) : base(source)
            => (ItemType, LengthExpression) = (itemType, lengthExpr);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

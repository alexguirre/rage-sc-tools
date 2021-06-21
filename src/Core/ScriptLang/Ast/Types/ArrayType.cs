namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;

    /// <summary>
    /// Represents an array type of constant size.
    /// </summary>
    public sealed class ArrayType : BaseArrayType
    {
        public int Length { get; set; }
        public IExpression LengthExpression { get; set; }

        public override int SizeOf => 1 + ItemType.SizeOf * Length;

        public ArrayType(SourceRange source, IType itemType, IExpression lengthExpr) : base(source, itemType)
            => LengthExpression = lengthExpr;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other)
            => other is ArrayType otherArray && Length == otherArray.Length && ItemType.Equivalent(otherArray.ItemType);

        public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs is ErrorType;
    }
}

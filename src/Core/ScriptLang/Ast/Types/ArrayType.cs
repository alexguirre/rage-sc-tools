namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;
    using ScTools.ScriptLang.Ast.Expressions;

    /// <summary>
    /// Represents an array type of constant size.
    /// </summary>
    public sealed class ArrayType : BaseType, IArrayType
    {
        public IType ItemType { get; set; }
        public int Length { get; set; }
        public IExpression LengthExpression { get; set; }

        public override int SizeOf => 1 + ItemType.SizeOf * Length;

        public ArrayType(SourceRange source, IType itemType, IExpression lengthExpr) : base(source)
            => (ItemType, LengthExpression) = (itemType, lengthExpr);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other)
            => other is ArrayType otherArray && Length == otherArray.Length && ItemType.Equivalent(otherArray.ItemType);

        public override bool CanAssign(IType rhs) => rhs is ErrorType;

        public override (IType Type, bool LValue) FieldAccess(string fieldName, SourceRange source, DiagnosticsReport diagnostics)
        {
            if (Parser.CaseInsensitiveComparer.Equals(fieldName, "length"))
            {
                return (new IntType(source), false);
            }
            else
            {
                return (new ErrorType(source, diagnostics, $"Unknown field '{fieldName}'"), false);
            }
        }

        public override IType Indexing(IType index, SourceRange source, DiagnosticsReport diagnostics)
        {
            var expectedIndexTy = new IntType(source);
            if (!expectedIndexTy.CanAssign(index))
            {
                return new ErrorType(source, diagnostics, $"Expected type '{expectedIndexTy}' as array index, found '{index}'");
            }

            return ItemType;
        }
    }
}

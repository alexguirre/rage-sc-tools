namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;

    public sealed class FloatType : BaseType
    {
        public override int SizeOf => 1;

        public FloatType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool CanAssign(IType rhs) => rhs is FloatType or NullType or ErrorType;
    }
}

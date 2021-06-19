namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;

    public sealed class StringType : BaseType
    {
        public override int SizeOf => 1;

        public StringType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is StringType;

        public override bool CanAssign(IType rhs) => rhs is StringType or NullType or ErrorType;
    }
}

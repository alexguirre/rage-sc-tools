namespace ScTools.ScriptLang.Ast.Types
{
    /// <summary>
    /// Represents the type of the NULL expression.
    /// </summary>
    public sealed class NullType : BaseType
    {
        public override int SizeOf => 1;

        public NullType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is NullType;
    }
}

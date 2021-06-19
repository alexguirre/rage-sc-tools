namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Expressions;

    /// <summary>
    /// Represents an array type of constant size.
    /// </summary>
    public sealed class AnyType : BaseType
    {
        public override int SizeOf => 1;

        public AnyType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is AnyType;
    }
}

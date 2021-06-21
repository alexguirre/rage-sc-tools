namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Errors;

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

        // ANY can take the value of any type with size 1
        public override bool CanAssign(IType rhs, bool rhsIsLValue) => rhs.ByValue is { SizeOf: 1 } or ErrorType;

        // ANY& can be binded to all other types
        public override bool CanBindRefTo(IType other) => true;
    }
}

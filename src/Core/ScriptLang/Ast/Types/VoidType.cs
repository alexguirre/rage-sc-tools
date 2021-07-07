namespace ScTools.ScriptLang.Ast.Types
{
    /// <summary>
    /// Represents the return type of a procedure.
    /// </summary>
    public sealed class VoidType : BaseType
    {
        public override int SizeOf => 0;

        public VoidType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is VoidType;
    }
}

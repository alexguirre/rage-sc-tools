namespace ScTools.ScriptLang.Ast.Types
{
    public sealed class RefType : BaseType
    {
        public IType PointeeType { get; set; }

        public override int SizeOf => 1;

        public RefType(SourceRange source, IType pointeeType) : base(source)
            => PointeeType = pointeeType;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);

        public override bool Equivalent(IType other) => other is RefType otherRef && PointeeType.Equivalent(otherRef.PointeeType);
    }
}

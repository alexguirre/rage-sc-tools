namespace ScTools.ScriptLang.Ast.Types
{
    public sealed class FloatType : BaseType
    {
        public override int SizeOf => 1;

        public FloatType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

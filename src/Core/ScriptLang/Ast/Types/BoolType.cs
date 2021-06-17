namespace ScTools.ScriptLang.Ast.Types
{
    public sealed class BoolType : BaseType
    {
        public override int SizeOf => 1;

        public BoolType(SourceRange source) : base(source) { }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

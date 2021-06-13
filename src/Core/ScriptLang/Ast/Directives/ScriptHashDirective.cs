namespace ScTools.ScriptLang.Ast.Directives
{
    public sealed class ScriptHashDirective : BaseDirective
    {
        public int Hash { get; set; }

        public ScriptHashDirective(SourceRange source, int hash) : base(source)
            => Hash = hash;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

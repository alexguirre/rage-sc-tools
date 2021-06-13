namespace ScTools.ScriptLang.Ast.Directives
{
    public sealed class UsingDirective : BaseDirective
    {
        public string Path { get; set; }

        public UsingDirective(SourceRange source, string path) : base(source)
            => Path = path;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

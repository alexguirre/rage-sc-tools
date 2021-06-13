namespace ScTools.ScriptLang.Ast.Directives
{
    public sealed class ScriptNameDirective : BaseDirective
    {
        public string Name { get; set; }

        public ScriptNameDirective(SourceRange source, string name) : base(source)
            => Name = name;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

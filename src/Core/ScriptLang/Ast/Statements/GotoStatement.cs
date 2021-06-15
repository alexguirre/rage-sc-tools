namespace ScTools.ScriptLang.Ast.Statements
{
    public sealed class GotoStatement : BaseStatement
    {
        public string LabelName { get; set; }

        public GotoStatement(SourceRange source, string labelName) : base(source)
            => LabelName = labelName;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

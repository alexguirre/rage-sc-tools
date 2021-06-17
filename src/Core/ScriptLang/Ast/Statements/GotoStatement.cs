namespace ScTools.ScriptLang.Ast.Statements
{
    using ScTools.ScriptLang.Ast.Declarations;

    public sealed class GotoStatement : BaseStatement
    {
        public string LabelName { get; set; }
        public LabelDeclaration? Label { get; set; }

        public GotoStatement(SourceRange source, string labelName) : base(source)
            => LabelName = labelName;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

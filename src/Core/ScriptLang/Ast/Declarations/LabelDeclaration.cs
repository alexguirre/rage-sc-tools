namespace ScTools.ScriptLang.Ast.Declarations
{
    using ScTools.ScriptLang.Ast.Statements;

    public sealed class LabelDeclaration : BaseNode, ILabelDeclaration, IStatement
    {
        public string Name { get; set; }

        public LabelDeclaration(SourceRange source, string name) : base(source)
            => Name = name;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

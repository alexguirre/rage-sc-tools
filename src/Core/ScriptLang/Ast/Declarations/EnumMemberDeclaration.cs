namespace ScTools.ScriptLang.Ast.Declarations
{
    using ScTools.ScriptLang.Ast.Expressions;

    public class EnumMemberDeclaration : BaseDeclaration
    {
        public int Value { get; set; }
        public IExpression? Initializer { get; set; }

        public EnumMemberDeclaration(SourceRange source, string name, IExpression? initializer) : base(source, name)
            => Initializer = initializer;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

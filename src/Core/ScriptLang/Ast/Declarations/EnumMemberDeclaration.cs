namespace ScTools.ScriptLang.Ast.Declarations
{
    using ScTools.ScriptLang.Ast.Expressions;
    using ScTools.ScriptLang.Ast.Types;

    public sealed class EnumMemberDeclaration : BaseValueDeclaration
    {
        public int Value { get; set; }
        public IExpression? Initializer { get; set; }

        public EnumMemberDeclaration(SourceRange source, string name, EnumType type, IExpression? initializer) : base(source, name, type)
            => Initializer = initializer;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

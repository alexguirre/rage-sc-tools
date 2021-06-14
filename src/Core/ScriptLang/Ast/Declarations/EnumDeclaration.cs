namespace ScTools.ScriptLang.Ast.Declarations
{
    using System.Collections.Generic;

    public class EnumDeclaration : BaseDeclaration
    {
        public IList<EnumMemberDeclaration> Members { get; set; }

        public EnumDeclaration(SourceRange source, string name, IEnumerable<EnumMemberDeclaration> members) : base(source, name)
            => Members = new List<EnumMemberDeclaration>(members);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

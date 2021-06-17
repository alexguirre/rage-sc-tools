namespace ScTools.ScriptLang.Ast.Declarations
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Types;

    public sealed class EnumDeclaration : BaseTypeDeclaration
    {
        public IList<EnumMemberDeclaration> Members { get; set; }

        public EnumDeclaration(SourceRange source, string name, IEnumerable<EnumMemberDeclaration> members) : base(source, name)
        {
            Members = new List<EnumMemberDeclaration>(members);
            foreach (var m in Members)
            {
                m.Type = new EnumType(m.Source, this);
            }
        }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

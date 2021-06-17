namespace ScTools.ScriptLang.Ast.Declarations
{
    using System.Collections.Generic;

    using ScTools.ScriptLang.Ast.Types;

    public sealed class EnumDeclaration : BaseTypeDeclaration
    {
        public IList<EnumMemberDeclaration> Members { get; set; } = new List<EnumMemberDeclaration>();

        public EnumDeclaration(SourceRange source, string name) : base(source, name) { }

        public override EnumType CreateType(SourceRange source) => new(source, this);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

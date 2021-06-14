using ScTools.ScriptLang.Ast.Declarations;

namespace ScTools.ScriptLang.Ast.Types
{
    public sealed class EnumType : BaseType
    {
        public override int SizeOf => 1;
        public EnumDeclaration Declaration { get; set; }

        public EnumType(SourceRange source, EnumDeclaration declaration) : base(source)
            => Declaration = declaration;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

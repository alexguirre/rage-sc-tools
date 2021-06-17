namespace ScTools.ScriptLang.Ast.Types
{
    using System.Linq;

    using ScTools.ScriptLang.Ast.Declarations;

    public sealed class StructType : BaseType
    {
        public override int SizeOf => Declaration.Fields.Sum(f => f.Type.SizeOf);
        public StructDeclaration Declaration { get; set; }

        public StructType(SourceRange source, StructDeclaration declaration) : base(source)
            => Declaration = declaration;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

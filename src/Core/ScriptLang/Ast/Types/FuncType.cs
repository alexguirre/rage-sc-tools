namespace ScTools.ScriptLang.Ast.Types
{
    using ScTools.ScriptLang.Ast.Declarations;

    public sealed class FuncType : BaseType
    {
        public override int SizeOf => 1;
        public FuncProtoDeclaration Declaration { get; set; }

        public FuncType(SourceRange source, FuncProtoDeclaration declaration) : base(source)
            => Declaration = declaration;

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

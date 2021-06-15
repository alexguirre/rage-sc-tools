namespace ScTools.ScriptLang.Ast.Declarations
{
    /// <summary>
    /// Represents a function or procedure prototype declaration.
    /// </summary>
    public sealed class FuncProtoDeclaration : BaseTypeDeclaration
    {
        public FuncProtoDeclaration(SourceRange source, string name) : base(source, name) {}

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

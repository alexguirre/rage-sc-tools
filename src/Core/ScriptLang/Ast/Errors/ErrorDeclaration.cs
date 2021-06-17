namespace ScTools.ScriptLang.Ast.Errors
{
    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Types;

    public sealed class ErrorDeclaration : BaseNode, IValueDeclaration, ITypeDeclaration, ILabelDeclaration, IError
    {
        public string Name { get; set; }
        public IType Type { get; set; }

        public ErrorDeclaration(SourceRange source) : base(source)
            => (Name, Type) = ("#ERROR#", new ErrorType(source));

        public IType CreateType(SourceRange source) => new ErrorType(source);

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

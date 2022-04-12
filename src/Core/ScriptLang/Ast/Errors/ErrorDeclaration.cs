namespace ScTools.ScriptLang.Ast.Errors;

using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Types;

public sealed class ErrorDeclaration : BaseError, IDeclaration, IValueDeclaration, ITypeDeclaration
{
    public string Name { get; set; }
    public IType Type { get; set; }

    public ErrorDeclaration(Diagnostic diagnostic) : base(diagnostic)
        => (Name, Type) = ("#ERROR#", new ErrorType(Diagnostic));

    public ErrorDeclaration(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source, diagnostics, message)
        => (Name, Type) = ("#ERROR#", new ErrorType(source, Diagnostic));

    public IType CreateType(SourceRange source) => new ErrorType(source, Diagnostic);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

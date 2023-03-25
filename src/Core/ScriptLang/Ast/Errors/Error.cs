using System.Collections.Generic;

namespace ScTools.ScriptLang.Ast.Errors;

public interface IError : INode
{
    Diagnostic Diagnostic { get; }
}

public abstract class BaseError : BaseNode, IError
{
    public Diagnostic Diagnostic { get; }

    public BaseError(Diagnostic diagnostic, IEnumerable<Token> tokens, IEnumerable<INode> children) : base(tokens, children)
    {
        Diagnostic = diagnostic;
    }

    public BaseError(SourceRange source, Diagnostic diagnostic) : base(source)
    {
        Diagnostic = diagnostic;
    }

    public BaseError(SourceRange source, DiagnosticsReport diagnostics, string message) : base(source)
    {
        Diagnostic = new Diagnostic(-1, DiagnosticSeverity.Error, message, source);
        diagnostics.Add(Diagnostic);
    }
}

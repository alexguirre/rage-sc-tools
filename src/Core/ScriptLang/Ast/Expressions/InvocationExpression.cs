namespace ScTools.ScriptLang.Ast.Expressions;

using System.Collections.Generic;
using System.Linq;

using ScTools.ScriptLang.Ast.Statements;

public sealed class InvocationExpression : BaseExpression, IStatement
{
    public string? Label { get; set; }
    public IExpression Callee { get; set; }
    public List<IExpression> Arguments { get; set; }

    public InvocationExpression(Token openParen, Token closeParen, IExpression callee, IEnumerable<IExpression> arguments) : base(openParen, closeParen)
        => (Callee, Arguments) = (callee, arguments.ToList());
    public InvocationExpression(SourceRange source, IExpression callee, IEnumerable<IExpression> arguments) : base(source)
        => (Callee, Arguments) = (callee, arguments.ToList());

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(SizeOfExpression)} {{ {nameof(Callee)} = {Callee.DebuggerDisplay}, Arguments: [{string.Join(", ", Arguments.Select(a => a.DebuggerDisplay))}] }}";
}

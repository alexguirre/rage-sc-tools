namespace ScTools.ScriptLang.Ast.Expressions;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast.Statements;

public sealed class InvocationExpression : BaseExpression, IStatement
{
    public string? Label { get; set; }
    public IExpression Callee => (IExpression)Children[0];
    public ImmutableArray<IExpression> Arguments { get; }

    public InvocationExpression(Token openParen, Token closeParen, IExpression callee, IEnumerable<IExpression> arguments)
        : base(OfTokens(openParen, closeParen), OfChildren(callee).AddRange(arguments))
    {
        Debug.Assert(openParen.Kind is TokenKind.OpenParen);
        Debug.Assert(closeParen.Kind is TokenKind.CloseParen);
        Arguments = arguments.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(InvocationExpression)} {{ {nameof(Callee)} = {Callee.DebuggerDisplay}, {nameof(Arguments)} = [{string.Join(", ", Arguments.Select(a => a.DebuggerDisplay))}] }}";
}

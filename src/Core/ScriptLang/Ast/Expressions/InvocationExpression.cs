namespace ScTools.ScriptLang.Ast.Expressions;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast.Statements;

public sealed partial class InvocationExpression : BaseExpression
{
    public IExpression Callee => (IExpression)Children[0];
    public ImmutableArray<IExpression> Arguments { get; }
    public Token OpenParen => Tokens[0];
    public Token CloseParen => Tokens[1];

    public InvocationExpression(Token openParen, Token closeParen, IExpression callee, IEnumerable<IExpression> arguments)
        : base(OfTokens(openParen, closeParen), OfChildren(callee).Concat(arguments))
    {
        Debug.Assert(openParen.Kind is TokenKind.OpenParen);
        Debug.Assert(closeParen.Kind is TokenKind.CloseParen);
        Arguments = arguments.ToImmutableArray();
    }

    public override string DebuggerDisplay =>
        $@"{nameof(InvocationExpression)} {{ {nameof(Callee)} = {Callee.DebuggerDisplay}, {nameof(Arguments)} = [{string.Join(", ", Arguments.Select(a => a.DebuggerDisplay))}] }}";
}

namespace ScTools.ScriptLang.Ast.Expressions;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast.Statements;

public sealed partial class InvocationExpression : BaseExpression, IStatement
{
    public Label? Label { get; }
    public IExpression Callee => (IExpression)Children[0];
    public ImmutableArray<IExpression> Arguments { get; }
    public Token OpenParen => Tokens[0];
    public Token CloseParen => Tokens[1];

    public InvocationExpression(Token openParen, Token closeParen, IExpression callee, IEnumerable<IExpression> arguments, Label? label = null)
        : base(OfTokens(openParen, closeParen), OfChildren(callee).Concat(arguments).AppendIfNotNull(label))
    {
        Debug.Assert(openParen.Kind is TokenKind.OpenParen);
        Debug.Assert(closeParen.Kind is TokenKind.CloseParen);
        Arguments = arguments.ToImmutableArray();
        Label = label;
    }

    internal InvocationExpression WithLabel(Label? label)
        => new(Tokens[0], Tokens[1], Callee, Arguments, label);

    public override string DebuggerDisplay =>
        $@"{nameof(InvocationExpression)} {{ {nameof(Callee)} = {Callee.DebuggerDisplay}, {nameof(Arguments)} = [{string.Join(", ", Arguments.Select(a => a.DebuggerDisplay))}] }}";
}

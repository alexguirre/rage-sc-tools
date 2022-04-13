namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class RepeatStatement : BaseStatement, ILoopStatement
{
    public IExpression Limit => (IExpression)Children[0];
    public IExpression Counter => (IExpression)Children[1];
    public ImmutableArray<IStatement> Body { get; }
    public LoopStatementSemantics Semantics { get; set; }
    BreakableStatementSemantics ISemanticNode<BreakableStatementSemantics>.Semantics
    {
        get => new(Semantics.ExitLabel);
        set => Semantics = Semantics with { ExitLabel = value.ExitLabel };
    }

    public RepeatStatement(Token repeatKeyword, Token endrepeatKeyword, IExpression limit, IExpression counter, IEnumerable<IStatement> body)
        : base(OfTokens(repeatKeyword, endrepeatKeyword), OfChildren(limit, counter).AddRange(body))
    {
        Debug.Assert(repeatKeyword.Kind is TokenKind.REPEAT);
        Debug.Assert(endrepeatKeyword.Kind is TokenKind.ENDREPEAT);
        Body = body.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(RepeatStatement)} {{ {nameof(Limit)} = {Limit.DebuggerDisplay}, {nameof(Counter)} = {Counter.DebuggerDisplay}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

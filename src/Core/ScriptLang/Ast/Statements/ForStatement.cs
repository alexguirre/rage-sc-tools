namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public sealed partial class ForStatement : BaseStatement, ILoopStatement
{
    public IExpression Counter => (IExpression)Children[0];
    public IExpression Initializer => (IExpression)Children[1];
    public IExpression Limit => (IExpression)Children[2];
    public ImmutableArray<IStatement> Body { get; }
    public LoopStatementSemantics Semantics { get; set; }
    BreakableStatementSemantics ISemanticNode<BreakableStatementSemantics>.Semantics
    {
        get => new(Semantics.ExitLabel);
        set => Semantics = Semantics with { ExitLabel = value.ExitLabel };
    }

    public ForStatement(Token forKeyword, Token toKeyword, Token endforKeyword,
        IExpression counter, IExpression initializer, IExpression limit,
        IEnumerable<IStatement> body, Label? label)
        : base(OfTokens(forKeyword, toKeyword, endforKeyword), OfChildren(counter, initializer, limit).Concat(body), label)
    {
        Debug.Assert(forKeyword.Kind is TokenKind.FOR);
        Debug.Assert(toKeyword.Kind is TokenKind.TO);
        Debug.Assert(endforKeyword.Kind is TokenKind.ENDFOR);
        Body = body.ToImmutableArray();
    }

    public override string DebuggerDisplay =>
        $@"{nameof(ForStatement)} {{ {nameof(Counter)} = {Counter.DebuggerDisplay}, {nameof(Initializer)} = {Initializer.DebuggerDisplay}, {nameof(Limit)} = {Limit.DebuggerDisplay}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

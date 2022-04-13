namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class RepeatStatement : BaseStatement, ILoopStatement
{
    public IExpression Limit { get; set; }
    public IExpression Counter { get; set; }
    public List<IStatement> Body { get; set; } = new();
    public LoopStatementSemantics Semantics { get; set; }
    BreakableStatementSemantics ISemanticNode<BreakableStatementSemantics>.Semantics
    {
        get => new(Semantics.ExitLabel);
        set => Semantics = Semantics with { ExitLabel = value.ExitLabel };
    }

    public RepeatStatement(SourceRange source, IExpression limit, IExpression counter) : base(source)
        => (Limit, Counter) = (limit, counter);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(RepeatStatement)} {{ {nameof(Limit)} = {Limit.DebuggerDisplay}, {nameof(Counter)} = {Counter.DebuggerDisplay}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

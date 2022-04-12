namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class RepeatStatement : BaseStatement, ILoopStatement
{
    public IExpression Limit { get; set; }
    public IExpression Counter { get; set; }
    public List<IStatement> Body { get; set; } = new();
    public string? ExitLabel { get; set; }
    public string? BeginLabel { get; set; }
    public string? ContinueLabel { get; set; }

    public RepeatStatement(SourceRange source, IExpression limit, IExpression counter) : base(source)
        => (Limit, Counter) = (limit, counter);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(RepeatStatement)} {{ {nameof(Limit)} = {Limit.DebuggerDisplay}, {nameof(Counter)} = {Counter.DebuggerDisplay}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

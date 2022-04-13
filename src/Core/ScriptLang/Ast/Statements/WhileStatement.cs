namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class WhileStatement : BaseStatement, ILoopStatement
{
    public IExpression Condition { get; set; }
    public List<IStatement> Body { get; set; } = new();
    public LoopStatementSemantics Semantics { get; set; }
    BreakableStatementSemantics ISemanticNode<BreakableStatementSemantics>.Semantics
    {
        get => new(Semantics.ExitLabel);
        set => Semantics = Semantics with { ExitLabel = value.ExitLabel };
    }

    public WhileStatement(SourceRange source, IExpression condition) : base(source)
        => Condition = condition;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(WhileStatement)} {{ {nameof(Condition)} = {Condition.DebuggerDisplay}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";

}

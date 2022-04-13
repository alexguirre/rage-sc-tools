namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class SwitchStatement : BaseStatement, IBreakableStatement
{
    public IExpression Expression { get; set; }
    public List<SwitchCase> Cases { get; set; } = new();
    public BreakableStatementSemantics Semantics { get; set; }

    public SwitchStatement(SourceRange source, IExpression expression) : base(source)
        => Expression = expression;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(SwitchStatement)} {{ {nameof(Expression)} = {Expression.DebuggerDisplay}, {nameof(Cases)} = [{string.Join(", ", Cases.Select(a => a.DebuggerDisplay))}] }}";
}

public record struct SwitchCaseSemantics(string? Label);

public abstract class SwitchCase : BaseNode, ISemanticNode<SwitchCaseSemantics>
{
    public SwitchStatement Switch { get; set; }
    public List<IStatement> Body { get; set; } = new();
    public SwitchCaseSemantics Semantics { get; set; }

    public SwitchCase(SourceRange source, SwitchStatement @switch) : base(source)
        => Switch = @switch;
}

public sealed class ValueSwitchCase : SwitchCase
{
    public IExpression Value { get; set; }

    public ValueSwitchCase(SourceRange source, SwitchStatement @switch, IExpression value) : base(source, @switch)
        => Value = value;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(ValueSwitchCase)} {{ {nameof(Value)} = {Value.DebuggerDisplay}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

public sealed class DefaultSwitchCase : SwitchCase
{
    public DefaultSwitchCase(SourceRange source, SwitchStatement @switch) : base(source, @switch) { }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(DefaultSwitchCase)} {{ {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

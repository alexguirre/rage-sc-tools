namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class SwitchStatement : BaseStatement, IBreakableStatement
{
    public IExpression Expression => (IExpression)Children[0];
    public ImmutableArray<SwitchCase> Cases { get; }
    public BreakableStatementSemantics Semantics { get; set; }

    public SwitchStatement(Token switchKeyword, Token endswitchKeyword, IExpression expression, IEnumerable<SwitchCase> cases)
        : base(OfTokens(switchKeyword, endswitchKeyword), OfChildren(expression).AddRange(cases))
    {
        Debug.Assert(switchKeyword.Kind is TokenKind.SWITCH);
        Debug.Assert(endswitchKeyword.Kind is TokenKind.ENDSWITCH);
        Cases = cases.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(SwitchStatement)} {{ {nameof(Expression)} = {Expression.DebuggerDisplay}, {nameof(Cases)} = [{string.Join(", ", Cases.Select(a => a.DebuggerDisplay))}] }}";
}

public record struct SwitchCaseSemantics(string? Label);

public abstract class SwitchCase : BaseNode, ISemanticNode<SwitchCaseSemantics>
{
    public ImmutableArray<IStatement> Body { get; }
    public SwitchCaseSemantics Semantics { get; set; }

    public SwitchCase(IEnumerable<IStatement> body, ImmutableArray<Token> tokens, ImmutableArray<INode> children) : base(tokens, children)
    {
        Body = body.ToImmutableArray();
    }
}

public sealed class ValueSwitchCase : SwitchCase
{
    public IExpression Value => (IExpression)Children[0];

    public ValueSwitchCase(Token caseKeyword, IExpression value, IEnumerable<IStatement> body)
        : base(body, OfTokens(caseKeyword), OfChildren(value).AddRange(body))
    {
        Debug.Assert(caseKeyword.Kind is TokenKind.CASE);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(ValueSwitchCase)} {{ {nameof(Value)} = {Value.DebuggerDisplay}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

public sealed class DefaultSwitchCase : SwitchCase
{
    public DefaultSwitchCase(Token defaultKeyword, IEnumerable<IStatement> body)
        : base(body, OfTokens(defaultKeyword), OfChildren(body))
    {
        Debug.Assert(defaultKeyword.Kind is TokenKind.DEFAULT);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(DefaultSwitchCase)} {{ {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

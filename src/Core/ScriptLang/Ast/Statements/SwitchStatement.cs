namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public sealed partial class SwitchStatement : BaseStatement, IBreakableStatement
{
    public IExpression Expression => (IExpression)Children[0];
    public ImmutableArray<SwitchCase> Cases { get; }
    public BreakableStatementSemantics Semantics { get; set; }

    public SwitchStatement(Token switchKeyword, Token endswitchKeyword, IExpression expression, IEnumerable<SwitchCase> cases, Label? label)
        : base(OfTokens(switchKeyword, endswitchKeyword), OfChildren(expression).Concat(cases), label)
    {
        Debug.Assert(switchKeyword.Kind is TokenKind.SWITCH);
        Debug.Assert(endswitchKeyword.Kind is TokenKind.ENDSWITCH);
        Cases = cases.ToImmutableArray();
    }

    public override string DebuggerDisplay =>
        $@"{nameof(SwitchStatement)} {{ {nameof(Expression)} = {Expression.DebuggerDisplay}, {nameof(Cases)} = [{string.Join(", ", Cases.Select(a => a.DebuggerDisplay))}] }}";
}

public record struct SwitchCaseSemantics(string? Label, int? Value);

public abstract class SwitchCase : BaseNode, ISemanticNode<SwitchCaseSemantics>
{
    public ImmutableArray<IStatement> Body { get; }
    public SwitchCaseSemantics Semantics { get; set; }

    public SwitchCase(IEnumerable<IStatement> body, IEnumerable<Token> tokens, IEnumerable<INode> children)
        : base(tokens, children.Concat(body))
    {
        Body = body.ToImmutableArray();
    }
}

public sealed partial class ValueSwitchCase : SwitchCase
{
    public IExpression Value => (IExpression)Children[0];

    public ValueSwitchCase(Token caseKeyword, IExpression value, IEnumerable<IStatement> body)
        : base(body, OfTokens(caseKeyword), OfChildren(value))
    {
        Debug.Assert(caseKeyword.Kind is TokenKind.CASE);
    }

    public override string DebuggerDisplay =>
        $@"{nameof(ValueSwitchCase)} {{ {nameof(Value)} = {Value.DebuggerDisplay}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

public sealed partial class DefaultSwitchCase : SwitchCase
{
    public DefaultSwitchCase(Token defaultKeyword, IEnumerable<IStatement> body)
        : base(body, OfTokens(defaultKeyword), OfChildren())
    {
        Debug.Assert(defaultKeyword.Kind is TokenKind.DEFAULT);
    }

    public override string DebuggerDisplay =>
        $@"{nameof(DefaultSwitchCase)} {{ {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

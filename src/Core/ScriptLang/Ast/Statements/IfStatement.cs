namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public record struct IfStatementSemantics(string? ElseLabel, string? EndLabel);

public sealed class IfStatement : BaseStatement, ISemanticNode<IfStatementSemantics>
{
    public IExpression Condition => (IExpression)Children[0];
    public ImmutableArray<IStatement> Then { get; }
    public ImmutableArray<IStatement> Else { get; }
    public IfStatementSemantics Semantics { get; set; }

    public IfStatement(Token ifKeyword, Token endifKeyword, IExpression condition, IEnumerable<IStatement> thenBody)
        : base(OfTokens(ifKeyword, endifKeyword), OfChildren(condition).AddRange(thenBody))
    {
        Debug.Assert(ifKeyword.Kind is TokenKind.IF);
        Debug.Assert(endifKeyword.Kind is TokenKind.ENDIF);
        Then = thenBody.ToImmutableArray();
        Else = ImmutableArray<IStatement>.Empty;
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(IfStatement)} {{ {nameof(Condition)} = {Condition.DebuggerDisplay}, {nameof(Then)} = [{string.Join(", ", Then.Select(a => a.DebuggerDisplay))}], {nameof(Else)} = [{string.Join(", ", Else.Select(a => a.DebuggerDisplay))}] }}";
}

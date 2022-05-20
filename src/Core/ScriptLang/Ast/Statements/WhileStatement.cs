namespace ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ScTools.ScriptLang.Ast.Expressions;

public sealed partial class WhileStatement : BaseStatement, ILoopStatement
{
    public IExpression Condition => (IExpression)Children[0];
    public ImmutableArray<IStatement> Body { get; }
    public LoopStatementSemantics Semantics { get; set; }
    BreakableStatementSemantics ISemanticNode<BreakableStatementSemantics>.Semantics
    {
        get => new(Semantics.ExitLabel);
        set => Semantics = Semantics with { ExitLabel = value.ExitLabel };
    }

    public WhileStatement(Token whileKeyword, Token endwhileKeyword, IExpression condition, IEnumerable<IStatement> body, Label? label)
        : base(OfTokens(whileKeyword, endwhileKeyword), OfChildren(condition).Concat(body), label)
    {
        Debug.Assert(whileKeyword.Kind is TokenKind.WHILE);
        Debug.Assert(endwhileKeyword.Kind is TokenKind.ENDWHILE);
        Body = body.ToImmutableArray();
    }

    public override string DebuggerDisplay =>
        $@"{nameof(WhileStatement)} {{ {nameof(Condition)} = {Condition.DebuggerDisplay}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";

}

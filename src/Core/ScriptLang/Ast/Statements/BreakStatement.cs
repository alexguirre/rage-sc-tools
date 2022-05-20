namespace ScTools.ScriptLang.Ast.Statements;

using System.Diagnostics;

public record struct BreakStatementSemantics(IBreakableStatement? EnclosingStatement);

public sealed partial class BreakStatement : BaseStatement, ISemanticNode<BreakStatementSemantics>
{
    public BreakStatementSemantics Semantics { get; set; }

    public BreakStatement(Token breakToken, Label? label) : base(OfTokens(breakToken), OfChildren(), label)
        => Debug.Assert(breakToken.Kind is TokenKind.BREAK);
}

namespace ScTools.ScriptLang.Ast.Statements;

using System.Diagnostics;

public record struct ContinueStatementSemantics(ILoopStatement? EnclosingLoop);

public sealed partial class ContinueStatement : BaseStatement
{
    public ContinueStatementSemantics Semantics { get; set; }

    public ContinueStatement(Token continueToken, Label? label) : base(OfTokens(continueToken), OfChildren(), label)
        => Debug.Assert(continueToken.Kind is TokenKind.CONTINUE);
}

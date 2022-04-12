namespace ScTools.ScriptLang.Ast.Statements;

using System.Diagnostics;

public sealed class GotoStatement : BaseStatement
{
    public string TargetLabel => Tokens[1].Lexeme.ToString();
    public IStatement? Target { get; set; }

    public GotoStatement(Token gotoToken, Token targetLabelIdentifierToken)
        : base(OfTokens(gotoToken, targetLabelIdentifierToken), OfChildren())
    {
        Debug.Assert(gotoToken.Kind is TokenKind.GOTO);
        Debug.Assert(targetLabelIdentifierToken.Kind is TokenKind.Identifier);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(GotoStatement)} {{ {nameof(TargetLabel)} = {TargetLabel} }}";
}

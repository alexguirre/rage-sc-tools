namespace ScTools.ScriptLang.Ast.Statements;

public sealed class GotoStatement : BaseStatement
{
    public string TargetLabel { get; }
    public IStatement? Target { get; set; }

    public GotoStatement(Token gotoToken, Token targetLabelIdentifierToken) : base(gotoToken, targetLabelIdentifierToken)
    {
        System.Diagnostics.Debug.Assert(gotoToken.Kind is TokenKind.GOTO);
        System.Diagnostics.Debug.Assert(targetLabelIdentifierToken.Kind is TokenKind.Identifier);
        TargetLabel = targetLabelIdentifierToken.Lexeme.ToString();
    }
    public GotoStatement(SourceRange source, string labelName) : base(source)
        => TargetLabel = labelName;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

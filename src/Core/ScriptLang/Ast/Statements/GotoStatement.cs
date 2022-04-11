namespace ScTools.ScriptLang.Ast.Statements;

public sealed class GotoStatement : BaseStatement
{
    public string TargetLabel => Tokens[1].Lexeme.ToString();
    public IStatement? Target { get; set; }

    public GotoStatement(Token gotoToken, Token targetLabelIdentifierToken) : base(gotoToken, targetLabelIdentifierToken)
    {
        System.Diagnostics.Debug.Assert(gotoToken.Kind is TokenKind.GOTO);
        System.Diagnostics.Debug.Assert(targetLabelIdentifierToken.Kind is TokenKind.Identifier);
    }
    public GotoStatement(SourceRange source, string labelName) : base(source)
        => throw new System.NotImplementedException("deprecated constructor");

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

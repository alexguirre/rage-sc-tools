namespace ScTools.ScriptLang.Ast.Statements;

public sealed class ContinueStatement : BaseStatement
{
    public ILoopStatement? EnclosingLoop { get; set; }

    public ContinueStatement(Token continueToken) : base(continueToken)
        => System.Diagnostics.Debug.Assert(continueToken.Kind is TokenKind.CONTINUE);
    public ContinueStatement(SourceRange source) : base(source) {}

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

namespace ScTools.ScriptLang.Ast.Statements;

public sealed class BreakStatement : BaseStatement
{
    public IBreakableStatement? EnclosingStatement { get; set; }

    public BreakStatement(Token breakToken) : base(breakToken)
        => System.Diagnostics.Debug.Assert(breakToken.Kind is TokenKind.BREAK);
    public BreakStatement(SourceRange source) : base(source) {}

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

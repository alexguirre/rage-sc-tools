namespace ScTools.ScriptLang.Ast.Statements;

public sealed class EmptyStatement : BaseStatement
{
    public EmptyStatement(Label? label) : base(OfTokens(), OfChildren(), label) { }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

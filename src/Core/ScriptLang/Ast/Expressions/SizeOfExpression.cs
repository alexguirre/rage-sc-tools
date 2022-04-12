namespace ScTools.ScriptLang.Ast.Expressions;

public sealed class SizeOfExpression : BaseExpression
{
    public IExpression SubExpression { get; set; }

    public SizeOfExpression(Token sizeOfToken, Token openToken, Token closeToken, IExpression subExpression) : base(sizeOfToken, openToken, closeToken)
    { 
        System.Diagnostics.Debug.Assert(sizeOfToken.Kind is TokenKind.SIZE_OF);
        System.Diagnostics.Debug.Assert(openToken.Kind is TokenKind.OpenParen);
        System.Diagnostics.Debug.Assert(closeToken.Kind is TokenKind.CloseParen);
        SubExpression = subExpression;
    }
    public SizeOfExpression(SourceRange source, IExpression subExpression) : base(source)
        => SubExpression = subExpression;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(SizeOfExpression)} {{ {nameof(SubExpression)} = {SubExpression.DebuggerDisplay} }}";
}

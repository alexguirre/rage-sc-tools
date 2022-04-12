namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed class SizeOfExpression : BaseExpression
{
    public IExpression SubExpression => (IExpression)Children[0];

    public SizeOfExpression(Token sizeOfToken, Token openParen, Token closeParen, IExpression subExpression)
        : base(OfTokens(sizeOfToken, openParen, closeParen), OfChildren(subExpression))
    { 
        Debug.Assert(sizeOfToken.Kind is TokenKind.SIZE_OF);
        Debug.Assert(openParen.Kind is TokenKind.OpenParen);
        Debug.Assert(closeParen.Kind is TokenKind.CloseParen);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(SizeOfExpression)} {{ {nameof(SubExpression)} = {SubExpression.DebuggerDisplay} }}";
}

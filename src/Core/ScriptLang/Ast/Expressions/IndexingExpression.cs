namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed class IndexingExpression : BaseExpression
{
    public IExpression Array => (IExpression)Children[0];
    public IExpression Index => (IExpression)Children[1];

    public IndexingExpression(Token openBracket, Token closeBracket, IExpression array, IExpression index)
        : base(OfTokens(openBracket, closeBracket), OfChildren(array, index))
    {
        Debug.Assert(openBracket.Kind is TokenKind.OpenBracket);
        Debug.Assert(closeBracket.Kind is TokenKind.CloseBracket);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(IndexingExpression)} {{ {nameof(Array)} = {Array.DebuggerDisplay}, {nameof(Index)} = {Index.DebuggerDisplay} }}";
}

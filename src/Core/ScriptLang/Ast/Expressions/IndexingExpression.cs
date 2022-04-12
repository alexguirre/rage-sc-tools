namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed class IndexingExpression : BaseExpression
{
    public IExpression Array { get; set; }
    public IExpression Index { get; set; }

    public IndexingExpression(Token openBracket, Token closeBracket, IExpression array, IExpression index) : base(openBracket, closeBracket)
    {
        Debug.Assert(openBracket.Kind is TokenKind.OpenBracket);
        Debug.Assert(closeBracket.Kind is TokenKind.CloseBracket);
        Array = array;
        Index = index;
    }
    public IndexingExpression(SourceRange source, IExpression array, IExpression index) : base(source)
        => (Array, Index) = (array, index);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(IndexingExpression)} {{ {nameof(Array)} = {Array.DebuggerDisplay}, {nameof(Index)} = {Index.DebuggerDisplay} }}";
}

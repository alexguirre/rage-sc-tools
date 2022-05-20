namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed partial class IndexingExpression : BaseExpression
{
    public IExpression Array => (IExpression)Children[0];
    public IExpression Index => (IExpression)Children[1];

    public IndexingExpression(Token openBracket, Token closeBracket, IExpression array, IExpression index)
        : base(OfTokens(openBracket, closeBracket), OfChildren(array, index))
    {
        Debug.Assert(openBracket.Kind is TokenKind.OpenBracket);
        Debug.Assert(closeBracket.Kind is TokenKind.CloseBracket);
    }

    public override string DebuggerDisplay =>
        $@"{nameof(IndexingExpression)} {{ {nameof(Array)} = {Array.DebuggerDisplay}, {nameof(Index)} = {Index.DebuggerDisplay} }}";
}

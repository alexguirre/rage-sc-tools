namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed class VectorExpression : BaseExpression
{
    public IExpression X => (IExpression)Children[0];
    public IExpression Y => (IExpression)Children[1];
    public IExpression Z => (IExpression)Children[2];

    public VectorExpression(Token openToken, Token commaXYToken, Token commaYZToken, Token closeToken,
                            IExpression x, IExpression y, IExpression z)
        : base(OfTokens(openToken, commaXYToken, commaYZToken, closeToken), OfChildren(x, y, z))
    {
        Debug.Assert(openToken.Kind is TokenKind.LessThanLessThan);
        Debug.Assert(commaXYToken.Kind is TokenKind.Comma);
        Debug.Assert(commaYZToken.Kind is TokenKind.Comma);
        Debug.Assert(closeToken.Kind is TokenKind.GreaterThanGreaterThan);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(VectorExpression)} {{ {nameof(X)} = {X.DebuggerDisplay}, {nameof(Y)} = {Y.DebuggerDisplay}, {nameof(Z)} = {Z.DebuggerDisplay} }}";
}

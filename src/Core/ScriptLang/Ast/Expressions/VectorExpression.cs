namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed class VectorExpression : BaseExpression
{
    public IExpression X { get; set; }
    public IExpression Y { get; set; }
    public IExpression Z { get; set; }

    public VectorExpression(SourceRange source, IExpression x, IExpression y, IExpression z) : base(source)
        => (X, Y, Z) = (x, y, z);

    public VectorExpression(Token openToken, Token commaXYToken, Token commaYZToken, Token closeToken,
                            IExpression x, IExpression y, IExpression z)
        : base(openToken, commaXYToken, commaYZToken, closeToken)
    {
        Debug.Assert(openToken.Kind is TokenKind.LessThanLessThan);
        Debug.Assert(commaXYToken.Kind is TokenKind.Comma);
        Debug.Assert(commaYZToken.Kind is TokenKind.Comma);
        Debug.Assert(closeToken.Kind is TokenKind.GreaterThanGreaterThan);
        (X, Y, Z) = (x, y, z);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(VectorExpression)} {{ {nameof(X)} = {X.DebuggerDisplay}, {nameof(Y)} = {Y.DebuggerDisplay}, {nameof(Z)} = {Z.DebuggerDisplay} }}";
}

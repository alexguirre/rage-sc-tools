namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed class FieldAccessExpression : BaseExpression
{
    public IExpression SubExpression => (IExpression)Children[0];
    public string FieldName => Tokens[1].Lexeme.ToString();

    public FieldAccessExpression(Token dotToken, Token fieldNameIdentifierToken, IExpression lhs)
        : base(OfTokens(dotToken, fieldNameIdentifierToken), OfChildren(lhs))
    {
        Debug.Assert(dotToken.Kind is TokenKind.Dot);
        Debug.Assert(fieldNameIdentifierToken.Kind is TokenKind.Identifier);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);

    public override string DebuggerDisplay =>
        $@"{nameof(FieldAccessExpression)} {{ {nameof(SubExpression)} = {SubExpression.DebuggerDisplay}, {nameof(FieldName)} = {FieldName} }}";
}

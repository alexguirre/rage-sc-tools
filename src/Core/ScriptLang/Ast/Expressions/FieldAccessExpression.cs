namespace ScTools.ScriptLang.Ast.Expressions;

public sealed class FieldAccessExpression : BaseExpression
{
    public IExpression SubExpression { get; set; }
    public string FieldName { get; set; }

    public FieldAccessExpression(Token dotToken, Token fieldNameIdentifierToken, IExpression lhs) : base(dotToken, fieldNameIdentifierToken)
        => (SubExpression, FieldName) = (lhs, fieldNameIdentifierToken.Lexeme.ToString());
    public FieldAccessExpression(SourceRange source, IExpression subExpression, string fieldName) : base(source)
        => (SubExpression, FieldName) = (subExpression, fieldName);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(FieldAccessExpression)} {{ {nameof(SubExpression)} = {SubExpression.DebuggerDisplay}, {nameof(FieldName)} = {FieldName} }}";
}

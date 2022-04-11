namespace ScTools.ScriptLang.Ast.Expressions;

public sealed class FieldAccessExpression : BaseExpression
{
    public IExpression SubExpression { get; set; }
    public string FieldName { get; set; }

    public FieldAccessExpression(SourceRange source, IExpression subExpression, string fieldName) : base(source)
        => (SubExpression, FieldName) = (subExpression, fieldName);

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

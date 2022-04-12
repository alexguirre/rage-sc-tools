namespace ScTools.ScriptLang.Ast.Statements;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class ReturnStatement : BaseStatement
{
    public IExpression? Expression { get; set; }

    public ReturnStatement(Token returnToken, IExpression? expression) : base(returnToken)
    {
        System.Diagnostics.Debug.Assert(returnToken.Kind is TokenKind.RETURN);
        Expression = expression;
    }
    public ReturnStatement(SourceRange source, IExpression? expression) : base(source)
        => Expression = expression;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(ReturnStatement)} {{ {nameof(Expression)} = {Expression?.DebuggerDisplay} }}";
}

namespace ScTools.ScriptLang.Ast.Statements;

using ScTools.ScriptLang.Ast.Expressions;

public sealed partial class ExpressionStatement : BaseStatement
{
    public IExpression Expression => (IExpression)Children[0];

    public ExpressionStatement(IExpression expression, Label? label)
        : base(OfTokens(), OfChildren(expression), label)
    {
    }

    public override string DebuggerDisplay =>
        $@"{nameof(ExpressionStatement)} {{ {nameof(Expression)} = {Expression.DebuggerDisplay} }}";
}

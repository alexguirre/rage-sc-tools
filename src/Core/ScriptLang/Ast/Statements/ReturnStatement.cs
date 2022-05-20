namespace ScTools.ScriptLang.Ast.Statements;

using System.Diagnostics;
using ScTools.ScriptLang.Ast.Expressions;

public sealed partial class ReturnStatement : BaseStatement
{
    public IExpression? Expression => Children.Length > 0 ? (IExpression)Children[0] : null;

    public ReturnStatement(Token returnToken, IExpression? expression, Label? label)
        : base(OfTokens(returnToken), expression is null ? OfChildren() : OfChildren(expression), label)
    {
        Debug.Assert(returnToken.Kind is TokenKind.RETURN);
    }

    public override string DebuggerDisplay =>
        $@"{nameof(ReturnStatement)} {{ {nameof(Expression)} = {Expression?.DebuggerDisplay} }}";
}

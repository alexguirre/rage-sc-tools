namespace ScTools.ScriptLang.Ast.Expressions;

using System.Diagnostics;

public sealed partial class NullExpression : BaseExpression
{
    public NullExpression(Token nullToken)
        : base(OfTokens(nullToken), OfChildren())
    {
        Debug.Assert(nullToken.Kind is TokenKind.Null);
    }
}

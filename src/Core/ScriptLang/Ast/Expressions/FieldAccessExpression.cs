namespace ScTools.ScriptLang.Ast.Expressions;

using ScTools.ScriptLang.Types;

using System.Diagnostics;

public record struct FieldAccessExpressionSemantics(TypeInfo? Type, ValueKind ValueKind, ArgumentKind ArgumentKind, FieldInfo? Field);

public sealed partial class FieldAccessExpression : BaseExpression
{
    private FieldInfo? semanticsField;

    public IExpression SubExpression => (IExpression)Children[0];
    public Token FieldNameToken => Tokens[1];
    public string FieldName => FieldNameToken.Lexeme.ToString();
    public new FieldAccessExpressionSemantics Semantics
    {
        get => new(base.Semantics.Type, base.Semantics.ValueKind, base.Semantics.ArgumentKind, semanticsField);
        set
        {
            base.Semantics = new(value.Type, value.ValueKind, value.ArgumentKind);
            semanticsField = value.Field;
        }
    }

    public FieldAccessExpression(Token dotToken, Token fieldNameIdentifierToken, IExpression lhs)
        : base(OfTokens(dotToken, fieldNameIdentifierToken), OfChildren(lhs))
    {
        Debug.Assert(dotToken.Kind is TokenKind.Dot);
        Debug.Assert(fieldNameIdentifierToken.Kind is TokenKind.Identifier);
    }

    public override string DebuggerDisplay =>
        $@"{nameof(FieldAccessExpression)} {{ {nameof(SubExpression)} = {SubExpression.DebuggerDisplay}, {nameof(FieldName)} = {FieldName} }}";
}

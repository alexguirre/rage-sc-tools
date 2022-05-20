namespace ScTools.ScriptLang.Ast.Expressions;

using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Types;

using System.Diagnostics;

public record struct NameExpressionSemantics(TypeInfo? Type, ValueKind ValueKind, ArgumentKind ArgumentKind, IDeclaration? Declaration);

/// <summary>
/// Represents a reference to a <see cref="IDeclaration"/>.
/// </summary>
public sealed partial class NameExpression : BaseExpression, ISemanticNode<NameExpressionSemantics>
{
    private IDeclaration? semanticsDeclaration;

    public Token NameToken => Tokens[0];
    public string Name => NameToken.Lexeme.ToString();
    public new NameExpressionSemantics Semantics
    {
        get => new(base.Semantics.Type, base.Semantics.ValueKind, base.Semantics.ArgumentKind, semanticsDeclaration);
        set
        {
            base.Semantics = new(value.Type, value.ValueKind, value.ArgumentKind);
            semanticsDeclaration = value.Declaration;
        }
    }

    public NameExpression(Token identifierToken)
        : base(OfTokens(identifierToken), OfChildren())
    {
        Debug.Assert(identifierToken.Kind is TokenKind.Identifier);
    }

    public override string DebuggerDisplay =>
        $@"{nameof(NameExpression)} {{ {nameof(Name)} = {Name} }}";

}

namespace ScTools.ScriptLang.Ast.Expressions;

using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Semantics;
using ScTools.ScriptLang.Types;

using System.Diagnostics;

public record struct NameExpressionSemantics(TypeInfo? Type, ValueKind ValueKind, ArgumentKind ArgumentKind, ISymbol? Symbol);

/// <summary>
/// Represents a reference to a <see cref="ISymbol"/>.
/// </summary>
public sealed partial class NameExpression : BaseExpression, ISemanticNode<NameExpressionSemantics>
{
    private ISymbol? semanticsSymbol;

    public Token NameToken => Tokens[0];
    public string Name => NameToken.Lexeme.ToString();
    public new NameExpressionSemantics Semantics
    {
        get => new(base.Semantics.Type, base.Semantics.ValueKind, base.Semantics.ArgumentKind, semanticsSymbol);
        set
        {
            base.Semantics = new(value.Type, value.ValueKind, value.ArgumentKind);
            semanticsSymbol = value.Symbol;
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

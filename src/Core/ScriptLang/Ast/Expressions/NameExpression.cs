namespace ScTools.ScriptLang.Ast.Expressions;

using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Types;

using System.Diagnostics;

public record struct NameExpressionSemantics(IType? Type, bool IsLValue, bool IsConstant, IDeclaration? Declaration);

/// <summary>
/// Represents a reference to a <see cref="IDeclaration"/>.
/// </summary>
public sealed class NameExpression : BaseExpression, ISemanticNode<NameExpressionSemantics>
{
    private IDeclaration? semanticsDeclaration;

    public string Name => Tokens[0].Lexeme.ToString();
    public new NameExpressionSemantics Semantics
    {
        get => new(base.Semantics.Type, base.Semantics.IsLValue, base.Semantics.IsConstant, semanticsDeclaration);
        set
        {
            base.Semantics = new(value.Type, value.IsLValue, value.IsConstant);
            semanticsDeclaration = value.Declaration;
        }
    }

    public NameExpression(Token identifierToken)
        : base(OfTokens(identifierToken), OfChildren())
    {
        Debug.Assert(identifierToken.Kind is TokenKind.Identifier);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override string DebuggerDisplay =>
        $@"{nameof(NameExpression)} {{ {nameof(Name)} = {Name} }}";

}

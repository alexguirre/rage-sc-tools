namespace ScTools.ScriptLang.Ast.Expressions;

using ScTools.ScriptLang.Ast.Declarations;

using System.Diagnostics;

/// <summary>
/// Represents a reference to a <see cref="IDeclaration"/>.
/// </summary>
public sealed class DeclarationRefExpression : BaseExpression
{
    public string Name => Tokens[0].Lexeme.ToString();
    public IDeclaration? Declaration { get; set; }

    public DeclarationRefExpression(Token identifierToken)
        : base(OfTokens(identifierToken), OfChildren())
    {
        Debug.Assert(identifierToken.Kind is TokenKind.Identifier);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override string DebuggerDisplay =>
        $@"{nameof(DeclarationRefExpression)} {{ {nameof(Name)} = {Name} }}";
}

namespace ScTools.ScriptLang.Ast.Expressions;

using ScTools.ScriptLang.Ast.Declarations;

/// <summary>
/// Represents a reference to a <see cref="IDeclaration"/>.
/// </summary>
public sealed class DeclarationRefExpression : BaseExpression
{
    public string Name { get; set; }
    public IDeclaration? Declaration { get; set; }

    public DeclarationRefExpression(Token identifierToken) : base(identifierToken)
    {
        System.Diagnostics.Debug.Assert(identifierToken.Kind is TokenKind.Identifier);
        Name = identifierToken.Lexeme.ToString();
    }
    public DeclarationRefExpression(SourceRange source, string name) : base(source)
        => Name = name;

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override string DebuggerDisplay =>
        $@"{nameof(BoolLiteralExpression)} {{ {nameof(Name)} = {Name} }}";
}

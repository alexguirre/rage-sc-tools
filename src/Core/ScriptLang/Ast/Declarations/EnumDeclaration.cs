namespace ScTools.ScriptLang.Ast.Declarations;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

using ScTools.ScriptLang.Ast.Expressions;

public sealed class EnumDeclaration : BaseTypeDeclaration
{
    public override Token NameToken => Tokens[1];
    public ImmutableArray<EnumMemberDeclaration> Members { get; }

    public EnumDeclaration(Token enumKeyword, Token nameIdentifier, Token endenumKeyword, IEnumerable<EnumMemberDeclaration> members)
        : base(OfTokens(enumKeyword, nameIdentifier, endenumKeyword), OfChildren(members))
    {
        Debug.Assert(enumKeyword.Kind is TokenKind.ENUM);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(endenumKeyword.Kind is TokenKind.ENDENUM);

        Members = members.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);
}

public sealed class EnumMemberDeclaration : BaseValueDeclaration
{
    public override Token NameToken => Tokens[0];
    public IExpression? Initializer => Children.Length > 0 ? (IExpression)Children[0] : null;

    public EnumMemberDeclaration(Token nameIdentifier, IExpression? initializer)
        : base(OfTokens(nameIdentifier), OfChildren().AppendIfNotNull(initializer))
    {
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
    public override void Accept(IVisitor visitor) => visitor.Visit(this);
}

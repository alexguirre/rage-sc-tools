namespace ScTools.ScriptLang.Ast.Declarations;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

using ScTools.ScriptLang.Ast.Expressions;
using ScTools.ScriptLang.Ast.Types;

public sealed class EnumDeclaration : BaseTypeDeclaration_New
{
    public override string Name => Tokens[1].Lexeme.ToString();
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
}

public sealed class EnumMemberDeclaration : BaseValueDeclaration_New
{
    public override string Name => Tokens[0].Lexeme.ToString();
    public IExpression? Initializer => Children.Length > 0 ? (IExpression)Children[0] : null;

    public EnumMemberDeclaration(Token nameIdentifier, IExpression? initializer)
        : base(OfTokens(nameIdentifier), OfChildren().AppendIfNotNull(initializer))
    {
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

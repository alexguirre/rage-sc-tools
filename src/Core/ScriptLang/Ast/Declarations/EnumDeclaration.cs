﻿namespace ScTools.ScriptLang.Ast.Declarations;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

using ScTools.ScriptLang.Ast.Expressions;

public sealed partial class EnumDeclaration : BaseTypeDeclaration
{
    public override Token NameToken => Tokens[1];
    public ImmutableArray<EnumMemberDeclaration> Members { get; }

    public bool IsStrict => Tokens[0].Kind is TokenKind.STRICT_ENUM;
    public bool IsHash => Tokens[0].Kind is TokenKind.HASH_ENUM;

    public EnumDeclaration(Token enumKeyword, Token nameIdentifier, Token endenumKeyword, IEnumerable<EnumMemberDeclaration> members)
        : base(OfTokens(enumKeyword, nameIdentifier, endenumKeyword), OfChildren(members))
    {
        Debug.Assert(enumKeyword.Kind is TokenKind.ENUM or TokenKind.STRICT_ENUM or TokenKind.HASH_ENUM);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(endenumKeyword.Kind is TokenKind.ENDENUM);

        Members = members.ToImmutableArray();
    }
}

public sealed partial class EnumMemberDeclaration : BaseValueDeclaration
{
    public override Token NameToken => Tokens[0];
    public IExpression? Initializer => Children.Length > 0 ? (IExpression)Children[0] : null;

    public EnumMemberDeclaration(Token nameIdentifier, IExpression? initializer)
        : base(OfTokens(nameIdentifier), OfChildren().AppendIfNotNull(initializer))
    {
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
    }
}

﻿namespace ScTools.ScriptLang.Ast.Declarations;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

public sealed partial class StructDeclaration : BaseTypeDeclaration
{
    public override Token NameToken => Tokens[1];
    public ImmutableArray<VarDeclaration> Fields { get; }

    public StructDeclaration(Token structKeyword, Token nameIdentifier, Token endstructKeyword, IEnumerable<VarDeclaration> fields)
        : base(OfTokens(structKeyword, nameIdentifier, endstructKeyword), OfChildren(fields))
    {
        Debug.Assert(structKeyword.Kind is TokenKind.STRUCT);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(endstructKeyword.Kind is TokenKind.ENDSTRUCT);

        Fields = fields.ToImmutableArray();
    }
}

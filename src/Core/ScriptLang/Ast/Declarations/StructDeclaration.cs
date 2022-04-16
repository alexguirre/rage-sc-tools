namespace ScTools.ScriptLang.Ast.Declarations;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

public sealed class StructDeclaration : BaseTypeDeclaration_New
{
    public override string Name => Tokens[1].Lexeme.ToString();
    public ImmutableArray<VarDeclaration_New> Fields { get; }

    public StructDeclaration(Token structKeyword, Token nameIdentifier, Token endstructKeyword, IEnumerable<VarDeclaration_New> fields)
        : base(OfTokens(structKeyword, nameIdentifier, endstructKeyword), OfChildren(fields))
    {
        Debug.Assert(structKeyword.Kind is TokenKind.STRUCT);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(endstructKeyword.Kind is TokenKind.ENDSTRUCT);

        Fields = fields.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);
}

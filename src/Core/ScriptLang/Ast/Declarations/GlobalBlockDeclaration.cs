namespace ScTools.ScriptLang.Ast.Declarations;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

public sealed partial class GlobalBlockDeclaration : BaseNode, IDeclaration
{
    public Token NameToken => Tokens[1];
    public string Name => NameToken.Lexeme.ToString();
    public Token BlockIndexToken => Tokens[2];
    public int BlockIndex => BlockIndexToken.GetIntLiteral();
    public ImmutableArray<VarDeclaration> Vars { get; }

    public GlobalBlockDeclaration(Token globalsKeyword, Token nameIdentifier, Token blockIndex, Token endglobalsKeyword,
                                    IEnumerable<VarDeclaration> vars)
        : base(OfTokens(globalsKeyword, nameIdentifier, blockIndex, endglobalsKeyword), OfChildren(vars))
    {
        Debug.Assert(globalsKeyword.Kind is TokenKind.GLOBALS);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(blockIndex.Kind is TokenKind.Integer);
        Debug.Assert(endglobalsKeyword.Kind is TokenKind.ENDGLOBALS);

        Vars = vars.ToImmutableArray();
    }
}

namespace ScTools.ScriptLang.Ast.Declarations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;

    public sealed class GlobalBlockDeclaration : BaseNode, IDeclaration_New
    {
        public const int MaxBlockCount = 64; // limit hardcoded in the game .exe (and max value that fits in GLOBAL_U24* instructions)
        public const int MaxSize = 0x3FFFF;

        public string Name => Tokens[1].Lexeme.ToString();
        public int BlockIndex => Tokens[2].GetIntLiteral();
        public ImmutableArray<VarDeclaration_New> Vars { get; }

        public GlobalBlockDeclaration(Token globalKeyword, Token nameIdentifier, Token blockIndex, Token endglobalKeyword,
                                      IEnumerable<VarDeclaration_New> vars)
            : base(OfTokens(globalKeyword, nameIdentifier, blockIndex, endglobalKeyword), OfChildren(vars))
        {
            Debug.Assert(globalKeyword.Kind is TokenKind.GLOBAL);
            Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
            Debug.Assert(blockIndex.Kind is TokenKind.Integer);
            Debug.Assert(endglobalKeyword.Kind is TokenKind.ENDGLOBAL);

            Vars = vars.ToImmutableArray();
        }

        public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
            => visitor.Visit(this, param);
    }
}

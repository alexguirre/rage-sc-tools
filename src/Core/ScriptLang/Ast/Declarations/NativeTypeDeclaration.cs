namespace ScTools.ScriptLang.Ast.Declarations;

using System.Diagnostics;

public sealed partial class NativeTypeDeclaration : BaseTypeDeclaration
{
    public override Token NameToken => Tokens[1];

    public NativeTypeDeclaration(Token nativeKeyword, Token nameIdentifier)
        : base(OfTokens(nativeKeyword, nameIdentifier), OfChildren())
    {
        Debug.Assert(nativeKeyword.Kind is TokenKind.NATIVE);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
    }
}

namespace ScTools.ScriptLang.Ast.Declarations;

using System.Diagnostics;

public sealed partial class NativeTypeDeclaration : BaseTypeDeclaration
{
    public override Token NameToken => Tokens[1];
    public TypeName? BaseType { get; }

    public NativeTypeDeclaration(Token nativeKeyword, Token nameIdentifier, TypeName? baseType)
        : base(OfTokens(nativeKeyword, nameIdentifier), OfChildren().AppendIfNotNull(baseType))
    {
        Debug.Assert(nativeKeyword.Kind is TokenKind.NATIVE);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);

        BaseType = baseType;
    }

    public override string DebuggerDisplay =>
        $@"{nameof(NativeTypeDeclaration)} {{ {nameof(Name)} = {Name}, {nameof(BaseType)} = {BaseType?.DebuggerDisplay} }}";
}

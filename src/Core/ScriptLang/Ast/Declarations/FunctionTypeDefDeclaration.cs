namespace ScTools.ScriptLang.Ast.Declarations;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public sealed partial class FunctionTypeDefDeclaration : BaseTypeDeclaration
{
    public override Token NameToken => Tokens[2];
    public TypeName? ReturnType { get; }
    public ImmutableArray<VarDeclaration> Parameters { get; }

    public FunctionTypeDefDeclaration(Token typedefKeyword, Token procOrFuncKeyword, Token nameIdentifier, Token paramsOpenParen, Token paramsCloseParen,
                                      TypeName? returnType, IEnumerable<VarDeclaration> parameters)
        : base(OfTokens(typedefKeyword, procOrFuncKeyword, nameIdentifier, paramsOpenParen, paramsCloseParen),
               OfChildren().AppendIfNotNull(returnType).Concat(parameters))
    {
        Debug.Assert(typedefKeyword.Kind is TokenKind.TYPEDEF);
        if (returnType is null)
            Debug.Assert(procOrFuncKeyword.Kind is TokenKind.PROC);
        else
            Debug.Assert(procOrFuncKeyword.Kind is TokenKind.FUNC);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(paramsOpenParen.Kind is TokenKind.OpenParen);
        Debug.Assert(paramsCloseParen.Kind is TokenKind.CloseParen);

        ReturnType = returnType;
        Parameters = parameters.ToImmutableArray();
    }

    public override string DebuggerDisplay =>
        $@"{nameof(FunctionTypeDefDeclaration)} {{ {nameof(Name)} = {Name}, {nameof(ReturnType)} = {ReturnType?.DebuggerDisplay}, {nameof(Parameters)} = [{string.Join(", ", Parameters.Select(a => a.DebuggerDisplay))} }}";
}

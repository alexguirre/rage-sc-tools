namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Statements;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public sealed partial class NativeFunctionDeclaration : BaseValueDeclaration
{
    public override Token NameToken => Tokens[2];
    public TypeName? ReturnType { get; }
    public ImmutableArray<VarDeclaration> Parameters { get; }

    public NativeFunctionDeclaration(Token nativeKeyword, Token procOrFuncKeyword, Token nameIdentifier, Token paramsOpenParen, Token paramsCloseParen,
                               TypeName? returnType, IEnumerable<VarDeclaration> parameters)
        : base(OfTokens(nativeKeyword, procOrFuncKeyword, nameIdentifier, paramsOpenParen, paramsCloseParen),
               OfChildren().AppendIfNotNull(returnType).Concat(parameters))
    {
        Debug.Assert(nativeKeyword.Kind is TokenKind.NATIVE);
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
        $@"{nameof(NativeFunctionDeclaration)} {{ {nameof(Name)} = {Name}, {nameof(ReturnType)} = {ReturnType?.DebuggerDisplay}, {nameof(Parameters)} = [{string.Join(", ", Parameters.Select(a => a.DebuggerDisplay))} }}";
}

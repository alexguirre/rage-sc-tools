namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Statements;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public sealed class FunctionPointerDeclaration : BaseTypeDeclaration
{
    public override string Name => Tokens[1].Lexeme.ToString();
    public TypeName? ReturnType { get; }
    public ImmutableArray<VarDeclaration> Parameters { get; }

    public FunctionPointerDeclaration(Token procOrFuncPtrKeyword, Token nameIdentifier, Token paramsOpenParen, Token paramsCloseParen,
                                      TypeName? returnType, IEnumerable<VarDeclaration> parameters)
        : base(OfTokens(procOrFuncPtrKeyword, nameIdentifier, paramsOpenParen, paramsCloseParen),
               OfChildren().AppendIfNotNull(returnType).Concat(parameters))
    {
        if (returnType is null)
            Debug.Assert(procOrFuncPtrKeyword.Kind is TokenKind.PROCPTR);
        else
            Debug.Assert(procOrFuncPtrKeyword.Kind is TokenKind.FUNCPTR);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(paramsOpenParen.Kind is TokenKind.OpenParen);
        Debug.Assert(paramsCloseParen.Kind is TokenKind.CloseParen);

        ReturnType = returnType;
        Parameters = parameters.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(FunctionPointerDeclaration)} {{ {nameof(Name)} = {Name}, {nameof(ReturnType)} = {ReturnType?.DebuggerDisplay}, {nameof(Parameters)} = [{string.Join(", ", Parameters.Select(a => a.DebuggerDisplay))} }}";
}

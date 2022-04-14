namespace ScTools.ScriptLang.Ast.Declarations;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public sealed class FunctionSignature : BaseNode
{
    public string Name => Tokens[1].Lexeme.ToString();
    public ITypeName? ReturnType { get; }
    public ImmutableArray<VarDeclaration_New> Parameters { get; }

    public FunctionSignature(Token procOrFuncKeyword, Token nameIdentifier, Token openParen, Token closeParen,
                             ITypeName? returnType, IEnumerable<VarDeclaration_New> parameters)
        : base(OfTokens(procOrFuncKeyword, nameIdentifier, openParen, closeParen),
               returnType is null ? OfChildren(parameters) : OfChildren(returnType).Concat(parameters))
    {
        Debug.Assert(procOrFuncKeyword.Kind is TokenKind.PROC or TokenKind.FUNC);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(openParen.Kind is TokenKind.OpenParen);
        Debug.Assert(closeParen.Kind is TokenKind.CloseParen);
        Debug.Assert(parameters.All(p => p.Kind is VarKind.Parameter));
        ReturnType = returnType;
        Parameters = parameters.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => throw new NotImplementedException();// visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(FunctionSignature)} {{ {nameof(ReturnType)} = {ReturnType?.DebuggerDisplay}, {nameof(Parameters)} = [{string.Join(", ", Parameters.Select(a => a.DebuggerDisplay))}] }}";
}

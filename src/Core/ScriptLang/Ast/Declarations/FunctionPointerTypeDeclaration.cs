namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Statements;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public sealed class FunctionPointerTypeDeclaration : BaseTypeDeclaration
{
    public override Token NameToken => Tokens[1];
    public TypeName? ReturnType { get; }
    public ImmutableArray<VarDeclaration> Parameters { get; }

    public FunctionPointerTypeDeclaration(Token procOrFuncPtrKeyword, Token nameIdentifier, Token paramsOpenParen, Token paramsCloseParen,
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
    public override void Accept(IVisitor visitor) => visitor.Visit(this);

    public override string DebuggerDisplay =>
        $@"{nameof(FunctionPointerTypeDeclaration)} {{ {nameof(Name)} = {Name}, {nameof(ReturnType)} = {ReturnType?.DebuggerDisplay}, {nameof(Parameters)} = [{string.Join(", ", Parameters.Select(a => a.DebuggerDisplay))} }}";
}

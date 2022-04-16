namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Statements;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public sealed class FunctionDeclaration : BaseValueDeclaration
{
    public override string Name => Tokens[1].Lexeme.ToString();
    public TypeName? ReturnType { get; }
    public ImmutableArray<VarDeclaration> Parameters { get; }
    public ImmutableArray<IStatement> Body { get; }

    public FunctionDeclaration(Token procOrFuncKeyword, Token nameIdentifier, Token paramsOpenParen, Token paramsCloseParen, Token endKeyword,
                               TypeName? returnType, IEnumerable<VarDeclaration> parameters, IEnumerable<IStatement> body)
        : base(OfTokens(procOrFuncKeyword, nameIdentifier, paramsOpenParen, paramsCloseParen, endKeyword),
               OfChildren().AppendIfNotNull(returnType).Concat(parameters).Concat(body))
    {
        if (returnType is null)
            Debug.Assert(procOrFuncKeyword.Kind is TokenKind.PROC && endKeyword.Kind is TokenKind.ENDPROC);
        else
            Debug.Assert(procOrFuncKeyword.Kind is TokenKind.FUNC && endKeyword.Kind is TokenKind.ENDFUNC);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(paramsOpenParen.Kind is TokenKind.OpenParen);
        Debug.Assert(paramsCloseParen.Kind is TokenKind.CloseParen);

        ReturnType = returnType;
        Parameters = parameters.ToImmutableArray();
        Body = body.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(FunctionDeclaration)} {{ {nameof(Name)} = {Name}, {nameof(ReturnType)} = {ReturnType?.DebuggerDisplay}, {nameof(Parameters)} = [{string.Join(", ", Parameters.Select(a => a.DebuggerDisplay))}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

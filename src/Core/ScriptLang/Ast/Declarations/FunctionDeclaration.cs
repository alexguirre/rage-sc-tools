namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Statements;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public sealed partial class FunctionDeclaration : BaseValueDeclaration
{
    public override Token NameToken => Tokens[1];
    public TypeName? ReturnType { get; }
    public ImmutableArray<VarDeclaration> Parameters { get; }
    public ImmutableArray<IStatement> Body { get; }

    public bool IsDebugOnly => Tokens.Last().Kind is TokenKind.DEBUGONLY;

    public FunctionDeclaration(Token? debugonlyKeyword, Token procOrFuncKeyword, Token nameIdentifier, Token paramsOpenParen, Token paramsCloseParen, Token endKeyword,
                               TypeName? returnType, IEnumerable<VarDeclaration> parameters, IEnumerable<IStatement> body)
        : base(OfTokens(procOrFuncKeyword, nameIdentifier, paramsOpenParen, paramsCloseParen, endKeyword).AppendIfNotNull(debugonlyKeyword),
               OfChildren().AppendIfNotNull(returnType).Concat(parameters).Concat(body))
    {
        if (debugonlyKeyword.HasValue)
            Debug.Assert(debugonlyKeyword.Value.Kind is TokenKind.DEBUGONLY);
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

    public override string DebuggerDisplay =>
        $@"{nameof(FunctionDeclaration)} {{ {nameof(Name)} = {Name}, {nameof(ReturnType)} = {ReturnType?.DebuggerDisplay}, {nameof(Parameters)} = [{string.Join(", ", Parameters.Select(a => a.DebuggerDisplay))}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

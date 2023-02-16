namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Expressions;
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
    public IExpression? Id { get; }

    public bool IsDebugOnly => Tokens.Last().Kind is TokenKind.DEBUGONLY;

    public NativeFunctionDeclaration(Token nativeKeyword, Token? debugonlyKeyword, Token procOrFuncKeyword, Token nameIdentifier, Token paramsOpenParen, Token paramsCloseParen,
                               TypeName? returnType, IEnumerable<VarDeclaration> parameters, IExpression? id)
        : base(OfTokens(nativeKeyword, procOrFuncKeyword, nameIdentifier, paramsOpenParen, paramsCloseParen).AppendIfNotNull(debugonlyKeyword),
               OfChildren().AppendIfNotNull(returnType).Concat(parameters).AppendIfNotNull(id))
    {
        Debug.Assert(nativeKeyword.Kind is TokenKind.NATIVE);
        if (debugonlyKeyword.HasValue)
            Debug.Assert(debugonlyKeyword.Value.Kind is TokenKind.DEBUGONLY);
        if (returnType is null)
            Debug.Assert(procOrFuncKeyword.Kind is TokenKind.PROC);
        else
            Debug.Assert(procOrFuncKeyword.Kind is TokenKind.FUNC);
        Debug.Assert(nameIdentifier.Kind is TokenKind.Identifier);
        Debug.Assert(paramsOpenParen.Kind is TokenKind.OpenParen);
        Debug.Assert(paramsCloseParen.Kind is TokenKind.CloseParen);

        ReturnType = returnType;
        Parameters = parameters.ToImmutableArray();
        Id = id;
    }

    public override string DebuggerDisplay =>
        $@"{nameof(NativeFunctionDeclaration)} {{ {nameof(Name)} = {Name}, {nameof(ReturnType)} = {ReturnType?.DebuggerDisplay}, {nameof(Parameters)} = [{string.Join(", ", Parameters.Select(a => a.DebuggerDisplay))}], {nameof(Id)} = {Id?.DebuggerDisplay} }}";
}

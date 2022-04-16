namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public sealed class ScriptDeclaration : BaseNode, IDeclaration_New
{
    public string Name => Tokens[1].Lexeme.ToString();
    public ImmutableArray<VarDeclaration_New> Parameters { get; }
    public ImmutableArray<IStatement> Body { get; }

    public ScriptDeclaration(Token scriptKeyword, Token nameIdentifier, Token paramsOpenParen, Token paramsCloseParen, Token endscriptKeyword,
                             IEnumerable<VarDeclaration_New> parameters, IEnumerable<IStatement> body)
        : base(OfTokens(scriptKeyword, nameIdentifier, paramsOpenParen, paramsCloseParen, endscriptKeyword),
               OfChildren(parameters).Concat(body))
    {
        Debug.Assert(scriptKeyword.Kind is TokenKind.SCRIPT && endscriptKeyword.Kind is TokenKind.ENDSCRIPT);

        Parameters = parameters.ToImmutableArray();
        Body = body.ToImmutableArray();
    }

    public ScriptDeclaration(Token scriptKeyword, Token nameIdentifier, Token endscriptKeyword,
                             IEnumerable<IStatement> body)
        : base(OfTokens(scriptKeyword, nameIdentifier, endscriptKeyword),
               OfChildren(body))
    {
        Debug.Assert(scriptKeyword.Kind is TokenKind.SCRIPT && endscriptKeyword.Kind is TokenKind.ENDSCRIPT);

        Parameters = ImmutableArray<VarDeclaration_New>.Empty;
        Body = body.ToImmutableArray();
    }

    public override TReturn Accept<TReturn, TParam>(IVisitor<TReturn, TParam> visitor, TParam param)
        => visitor.Visit(this, param);

    public override string DebuggerDisplay =>
        $@"{nameof(ScriptDeclaration)} {{ {nameof(Name)} = {Name}, {nameof(Parameters)} = [{string.Join(", ", Parameters.Select(a => a.DebuggerDisplay))}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

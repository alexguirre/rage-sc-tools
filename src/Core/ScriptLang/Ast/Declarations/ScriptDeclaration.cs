namespace ScTools.ScriptLang.Ast.Declarations;

using ScTools.ScriptLang.Ast.Statements;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public record struct ScriptDeclarationSemantics(List<VarDeclaration>? UsedStatics);

public sealed partial class ScriptDeclaration : BaseNode, IDeclaration, ISemanticNode<ScriptDeclarationSemantics>
{
    public Token NameToken => Tokens[1];
    public string Name => NameToken.Lexeme.ToString();
    public ImmutableArray<VarDeclaration> Parameters { get; }
    public ImmutableArray<IStatement> Body { get; }
    public ScriptDeclarationSemantics Semantics { get; set; }

    public ScriptDeclaration(Token scriptKeyword, Token nameIdentifier, Token paramsOpenParen, Token paramsCloseParen, Token endscriptKeyword,
                             IEnumerable<VarDeclaration> parameters, IEnumerable<IStatement> body)
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

        Parameters = ImmutableArray<VarDeclaration>.Empty;
        Body = body.ToImmutableArray();
    }

    public override string DebuggerDisplay =>
        $@"{nameof(ScriptDeclaration)} {{ {nameof(Name)} = {Name}, {nameof(Parameters)} = [{string.Join(", ", Parameters.Select(a => a.DebuggerDisplay))}, {nameof(Body)} = [{string.Join(", ", Body.Select(a => a.DebuggerDisplay))}] }}";
}

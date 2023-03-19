namespace ScTools.LanguageServer.Handlers;

using ScTools.LanguageServer.Services;
using ScTools.ScriptLang.Ast.Declarations;
using ScTools.ScriptLang.Ast.Expressions;

public class GotoHandler : ILspRequestHandler<TextDocumentPositionParams, Location?>
{
    private readonly ILogger<GotoHandler> logger;
    private readonly ITextDocumentTracker textDocumentTracker;

    public string MethodName => Methods.TextDocumentDefinitionName;

    public GotoHandler(ITextDocumentTracker textDocumentTracker, ILogger<GotoHandler> logger)
    {
        this.logger = logger;
        this.textDocumentTracker = textDocumentTracker;
    }

    public void AddProvidedCapabilities(ServerCapabilities capabilities)
    {
        capabilities.DefinitionProvider = true;
    }

    public async Task<Location?> HandleAsync(TextDocumentPositionParams param, CancellationToken cancellationToken)
    {
        var pos = ProtocolConversions.FromLspPosition(param.Position);
        var ast = await textDocumentTracker.GetDocumentAstAsync(param.TextDocument.Uri);
        if (ast is null)
        {
            return null;
        }
        var node = AstNodeLocator.Locate(ast, pos);

        IDeclaration? decl = null;
        if (node is NameExpression nameExpr)
        {
            decl = nameExpr.Semantics.Symbol as IDeclaration;
        }
        else if (node is TypeName typeName)
        {
            var s = await textDocumentTracker.GetDocumentSemanticsAsync(param.TextDocument.Uri);
            if (s != null && s.GetSymbolUnchecked(typeName.Name, out var typeSymbol) && typeSymbol is IDeclaration typeDecl)
            {
                decl = typeDecl;
            }
        }

        if (decl == null)
        {
            return null;
        }

        var result = new Location
        {
            Uri = new Uri("file://" + decl.Location.FilePath, UriKind.Absolute),
            Range = ProtocolConversions.ToLspRange(decl.Location),
        };
        return result;
    }
}

namespace ScTools.LanguageServer.Handlers;

using ScTools.LanguageServer.Services;

using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

public class HoverHandler : ILspRequestHandler<TextDocumentPositionParams, Hover?>
{
    private readonly ITextDocumentTracker textDocumentTracker;

    public string MethodName => Methods.TextDocumentHoverName;

    public HoverHandler(ITextDocumentTracker textDocumentTracker)
    {
        this.textDocumentTracker = textDocumentTracker;
    }

    public void AddProvidedCapabilities(ServerCapabilities capabilities)
    {
        capabilities.HoverProvider = true;
    }

    public async Task<Hover?> HandleAsync(TextDocumentPositionParams param, CancellationToken cancellationToken)
    {
        var ast = await textDocumentTracker.GetDocumentAstAsync(param.TextDocument.Uri);
        if (ast is null)
        {
            return null;
        }

        var node = AstNodeLocator.Locate(ast, ProtocolConversions.FromLspPosition(param.Position));

        var result = new Hover
        {
            Contents = new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = node?.DebuggerDisplay ?? $"<no AST node at this position ({param.Position.Line}, {param.Position.Character})>",
            },
            Range = node is null ? new LspRange { Start = param.Position, End = param.Position } :
                                   ProtocolConversions.ToLspRange(node.Location),
        };

        return result;
    }
}

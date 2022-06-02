namespace ScTools.LanguageServer.Handlers;

using Microsoft.VisualStudio.LanguageServer.Protocol;

using ScTools.LanguageServer.Services;

using System.Threading;
using System.Threading.Tasks;

public class HoverHandler : ILspRequestHandler<TextDocumentPositionParams, Hover>
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

    public Task<Hover> HandleAsync(TextDocumentPositionParams param, CancellationToken cancellationToken)
    {
        var ast = textDocumentTracker.GetDocumentAst(param.TextDocument.Uri);
        var node = AstNodeLocator.Locate(ast, ProtocolConversions.FromLspPosition(param.Position));

        var result = new Hover
        {
            Contents = new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = node?.DebuggerDisplay ?? $"<no AST node at this position ({param.Position.Line}, {param.Position.Character})>",
            },
            Range = node is null ? new Range { Start = param.Position, End = param.Position } :
                                   ProtocolConversions.ToLspRange(node.Location),
        };

        return Task.FromResult(result);
    }
}

namespace ScTools.LanguageServer.Handlers;

using Microsoft.VisualStudio.LanguageServer.Protocol;

using ScTools.LanguageServer.Services;

using System.Threading;
using System.Threading.Tasks;

public class DidCloseDocumentHandler : ILspRequestHandler<DidCloseTextDocumentParams, object?>
{
    private readonly ITextDocumentTracker documentTracker;

    public string MethodName => Methods.TextDocumentDidCloseName;

    public DidCloseDocumentHandler(ITextDocumentTracker documentTracker)
    {
        this.documentTracker = documentTracker;
    }

    public void AddProvidedCapabilities(ServerCapabilities capabilities)
    {
        // nothing to add, DidOpenDocumentHandler already adds the necessary capabilities
    }

    public async Task<object?> HandleAsync(DidCloseTextDocumentParams param, CancellationToken cancellationToken)
    {
        await documentTracker.CloseDocumentAsync(param.TextDocument.Uri);
        return null;
    }
}

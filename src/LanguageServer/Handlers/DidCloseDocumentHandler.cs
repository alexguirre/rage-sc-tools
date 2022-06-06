namespace ScTools.LanguageServer.Handlers;

using ScTools.LanguageServer.Services;

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

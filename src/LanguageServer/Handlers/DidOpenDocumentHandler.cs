namespace ScTools.LanguageServer.Handlers;

using ScTools.LanguageServer.Services;

public class DidOpenDocumentHandler : ILspRequestHandler<DidOpenTextDocumentParams, object?>
{
    private readonly ITextDocumentTracker documentTracker;

    public string MethodName => Methods.TextDocumentDidOpenName;

    public DidOpenDocumentHandler(ITextDocumentTracker documentTracker)
    {
        this.documentTracker = documentTracker;
    }

    public void AddProvidedCapabilities(ServerCapabilities capabilities)
    {
        capabilities.TextDocumentSync = new TextDocumentSyncOptions
        {
            OpenClose = true,
            Change = TextDocumentSyncKind.Full,
        };
    }

    public async Task<object?> HandleAsync(DidOpenTextDocumentParams param, CancellationToken cancellationToken)
    {
        await documentTracker.OpenDocumentAsync(param.TextDocument.Uri, param.TextDocument.Text);
        return null;
    }
}

namespace ScTools.LanguageServer.Handlers;

using Microsoft.VisualStudio.LanguageServer.Protocol;

using ScTools.LanguageServer.Services;

using System.Threading;
using System.Threading.Tasks;

public class DidChangeDocumentHandler : ILspRequestHandler<DidChangeTextDocumentParams, object?>
{
    private readonly ITextDocumentTracker documentTracker;

    public string MethodName => Methods.TextDocumentDidChangeName;

    public DidChangeDocumentHandler(ITextDocumentTracker documentTracker)
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

    public async Task<object?> HandleAsync(DidChangeTextDocumentParams param, CancellationToken cancellationToken)
    {
        await documentTracker.UpdateDocumentAsync(param.TextDocument.Uri, param.ContentChanges[0].Text);
        return null;
    }
}

namespace ScTools.LanguageServer.Handlers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLspRequestHandlers(this IServiceCollection services)
        => services.AddSingleton<ILspRequestHandler, DidOpenDocumentHandler>()
                   .AddSingleton<ILspRequestHandler, DidChangeDocumentHandler>()
                   .AddSingleton<ILspRequestHandler, DidCloseDocumentHandler>()
                   .AddSingleton<ILspRequestHandler, HoverHandler>()
                   .AddSingleton<ILspRequestHandler, DocumentSymbolHandler>()
                   .AddSingleton<ILspRequestHandler, RenameHandler>();
}

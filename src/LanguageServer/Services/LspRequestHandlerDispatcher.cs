namespace ScTools.LanguageServer.Services;

using Newtonsoft.Json.Linq;

using ScTools.LanguageServer.Handlers;

using StreamJsonRpc;

public readonly record struct LspRequestReceivedEventArgs(ILspRequestHandler Handler, JToken Request);
public readonly record struct LspRequestHandledEventArgs(ILspRequestHandler Handler, object? Response);

public interface ILspRequestHandlerDispatcher
{
    void AddProvidedCapabilities(ServerCapabilities capabilities);
    void RegisterRpcMethods(JsonRpc rpc);
}

public class LspRequestHandlerDispatcher : ILspRequestHandlerDispatcher
{
    private readonly ImmutableArray<ILspRequestHandler> handlers;

    public LspRequestHandlerDispatcher(IEnumerable<ILspRequestHandler> handlers)
    {
        this.handlers = handlers.ToImmutableArray();
    }

    public void AddProvidedCapabilities(ServerCapabilities capabilities)
    {
        foreach (var handler in handlers)
        {
            handler.AddProvidedCapabilities(capabilities);
        }
    }
    
    public void RegisterRpcMethods(JsonRpc rpc)
    {
        foreach (var handler in handlers)
        {
            rpc.AddLocalRpcMethod(handler.MethodName, handler.HandleAsync);
        }
    }
}

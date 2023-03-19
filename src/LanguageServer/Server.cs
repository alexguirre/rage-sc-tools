namespace ScTools.LanguageServer;

using Newtonsoft.Json.Linq;

using StreamJsonRpc;
using ScTools.LanguageServer.Services;

public interface IServer
{
    void WaitForExit();
    void Exit();
    Task SendNotificationAsync<TIn>(LspNotification<TIn> method, TIn param);
}

internal sealed partial class Server : IServer, IDisposable
{
    private readonly ILogger<Server> logger;
    private readonly HeaderDelimitedMessageHandler messageHandler;
    private readonly JsonRpc rpc;
    private readonly ManualResetEvent disconnectEvent = new(initialState: false);
    private readonly ILspRequestHandlerDispatcher handlerDispatcher;

    public Server(IServerIOProvider io, ILspRequestHandlerDispatcher handlerDispatcher, ILogger<Server> logger)
    {
        this.logger = logger;
        this.handlerDispatcher = handlerDispatcher;

        var rpcTraceSource = new TraceSource("ScTools.LanguageServer.Server[RPC]", SourceLevels.Verbose | SourceLevels.ActivityTracing);

        messageHandler = new(io.Sender, io.Receiver);
        rpc = new(messageHandler, this);
        handlerDispatcher.RegisterRpcMethods(rpc);
        rpc.Disconnected += OnRpcDisconnected;
        rpc.ActivityTracingStrategy = new CorrelationManagerTracingStrategy
        {
            TraceSource = rpcTraceSource,
        };
        rpc.TraceSource = rpcTraceSource;
        rpc.StartListening();
    }

    public void WaitForExit()
    {
        disconnectEvent.WaitOne();
    }

    public void Exit()
    {
        disconnectEvent.Set();
    }

    private void OnRpcDisconnected(object? sender, JsonRpcDisconnectedEventArgs e)
    {
        Exit();
    }

    public void Dispose()
    {
        rpc.Dispose();
        disconnectEvent.Dispose();
    }

    public Task SendNotificationAsync<TIn>(LspNotification<TIn> method, TIn param)
    {
        return rpc.NotifyWithParameterObjectAsync(method.Name, param);
    }

    [JsonRpcMethod(Methods.InitializeName)]
    public InitializeResult InitializeHandler(JToken arg)
    {
        var capabilities = new ServerCapabilities();
        handlerDispatcher.AddProvidedCapabilities(capabilities);
        logger.LogInformation("Initialize with capabilities: {@capabilities}",  capabilities);
        return new InitializeResult { Capabilities = capabilities };
    }
}

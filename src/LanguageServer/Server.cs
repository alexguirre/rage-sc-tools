namespace ScTools.LanguageServer;

using System;
using System.IO;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.LanguageServer.Protocol;

using Newtonsoft.Json.Linq;

using StreamJsonRpc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ScTools.ScriptLang;
using ScTools.ScriptLang.Semantics;
using ScTools.LanguageServer.Services;

public interface IServer
{
    void WaitForExit();
    void Exit();
    Task SendNotificationAsync<TIn>(LspNotification<TIn> method, TIn param);
}

internal sealed partial class Server : IServer, IDisposable
{
    private readonly HeaderDelimitedMessageHandler messageHandler;
    private readonly JsonRpc rpc;
    private readonly ManualResetEvent disconnectEvent = new(initialState: false);
    private readonly ILspRequestHandlerDispatcher handlerDispatcher;

    public Server(IServerIOProvider io, ILspRequestHandlerDispatcher handlerDispatcher)
    {
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
        return new InitializeResult { Capabilities = capabilities };
    }
}

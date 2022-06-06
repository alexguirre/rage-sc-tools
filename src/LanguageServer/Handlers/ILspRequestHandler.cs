namespace ScTools.LanguageServer.Handlers;

using Newtonsoft.Json.Linq;

public interface ILspRequestHandler
{
    public string MethodName { get; }

    void AddProvidedCapabilities(ServerCapabilities capabilities);
    Task<object?> HandleAsync(JToken param, CancellationToken cancellationToken);
}

public interface ILspRequestHandler<TIn, TOut> : ILspRequestHandler
{
    async Task<object?> ILspRequestHandler.HandleAsync(JToken arg, CancellationToken cancellationToken)
    {
        var param = arg.ToObject<TIn>() ?? throw new ArgumentException("Invalid argument", nameof(arg));
        return await HandleAsync(param, cancellationToken);
    }

    Task<TOut> HandleAsync(TIn param, CancellationToken cancellationToken);
}

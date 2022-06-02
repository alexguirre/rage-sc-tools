namespace ScTools.LanguageServer.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.LanguageServer.Protocol;

using ScTools.ScriptLang;
using ScTools.ScriptLang.Semantics;

using System;
using System.Linq;
using System.Threading.Tasks;

public interface IDiagnosticsPublisher
{
    Task SendDiagnosticsAsync(Uri uri, DiagnosticsReport diagnostics);
}

public class DiagnosticsPublisher : IDiagnosticsPublisher
{
    private readonly Lazy<IServer> server;
    
    public DiagnosticsPublisher(IServiceProvider services)
    {
        server = new Lazy<IServer>(() => services.GetRequiredService<IServer>());
    }

    public async Task SendDiagnosticsAsync(Uri uri, DiagnosticsReport diagnostics)
    {
        var param = new PublishDiagnosticParams
        {
            Uri = uri,
            Diagnostics = diagnostics.AllDiagnostics.Select(ProtocolConversions.ToLspDiagnostic).ToArray(),
        };

        await server.Value.SendNotificationAsync(Methods.TextDocumentPublishDiagnostics, param);
    }
}

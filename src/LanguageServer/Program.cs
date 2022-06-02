namespace ScTools.LanguageServer;

using System;
using System.CommandLine;
using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

using ScTools.LanguageServer.Handlers;
using ScTools.LanguageServer.Services;

internal static class Program
{
    private static int Main(string[] args)
    {
        var rootCmd = new RootCommand("Language server for ScriptLang (.sc).")
        {
            new Option<bool>("--launch-debugger", () => false)
        };
        rootCmd.SetHandler<bool>(Run, rootCmd.Options[0]);
        return rootCmd.Invoke(args);
    }

    private static void Run(bool launchDebugger)
    {
        if (launchDebugger)
        {
            Debugger.Launch();
        }

        var services = new ServiceCollection()
            .AddLspRequestHandlers()
            .AddSingleton<ILspRequestHandlerDispatcher, LspRequestHandlerDispatcher>()
            .AddSingleton<ITextDocumentTracker, TextDocumentTracker>()
            .AddSingleton<IDiagnosticsPublisher, DiagnosticsPublisher>()
            .AddSingleton<IServerIOProvider, ServerStandardIOProvider>()
            .AddSingleton<IServer, Server>();
        using var serviceProvider = services.BuildServiceProvider();

        var server = serviceProvider.GetRequiredService<IServer>();
        server.WaitForExit();
    }
}

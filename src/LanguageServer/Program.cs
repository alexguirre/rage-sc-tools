﻿namespace ScTools.LanguageServer;

using System.CommandLine;
using System.Diagnostics;

using ScTools.LanguageServer.Handlers;
using ScTools.LanguageServer.Services;

//using Serilog;
//using Serilog.Extensions;

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
            .AddLogging(logging => 
                logging.AddDebug()
                       /*.AddSerilog(new LoggerConfiguration()
                            .WriteTo.File("log.txt")
                            .CreateLogger())*/)
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

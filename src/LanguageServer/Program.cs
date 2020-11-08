namespace ScTools.LanguageServer
{
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Threading;

    internal static class Program
    {
        private static int Main(string[] args)
        {
            var rootCmd = new RootCommand("Language server for ScriptLang (.sc).")
            {
                new Argument<int>("port", "Port to use."),
                new Option<bool>("--wait-for-debugger", () => false)
            };
            rootCmd.Handler = CommandHandler.Create<int, bool>(Run);

            return rootCmd.Invoke(args);
        }

        private static int Run(int port, bool waitForDebugger)
        {
            if (waitForDebugger)
            {
                while (!Debugger.IsAttached) Thread.Sleep(500);
                Debugger.Break();
            }

            using var tcp = new TcpClient("localhost", port);
            var tcpStream = tcp.GetStream();

            var server = new Server(tcpStream, tcpStream);

            server.WaitForShutdown();

            return server.ReadyForExit ? 0 : 1;
        }
    }
}

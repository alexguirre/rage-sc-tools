namespace ScTools.LanguageServer
{
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Net.Sockets;

    internal static class Program
    {
        private static int Main(string[] args)
        {
            var rootCmd = new RootCommand("Language server for ScriptLang (.sc).")
            {
                new Argument<int>("port", "Port to use.")
            };
            rootCmd.Handler = CommandHandler.Create<int>(Run);

            return rootCmd.Invoke(args);
        }

        private static int Run(int port)
        {
            using var tcp = new TcpClient("localhost", port);
            var tcpStream = tcp.GetStream();

            var server = new Server(tcpStream, tcpStream);

            server.WaitForShutdown();

            return server.ReadyForExit ? 0 : 1;
        }
    }
}

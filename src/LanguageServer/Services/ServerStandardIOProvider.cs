namespace ScTools.LanguageServer.Services;

using System.IO;

public class ServerStandardIOProvider : IServerIOProvider
{
    private bool isDisposed;

    public Stream Sender { get; }
    public Stream Receiver { get; }

    public ServerStandardIOProvider()
    {
        Receiver = Console.OpenStandardInput();
        Sender = Console.OpenStandardOutput();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                Sender.Dispose();
                Receiver.Dispose();
            }

            isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

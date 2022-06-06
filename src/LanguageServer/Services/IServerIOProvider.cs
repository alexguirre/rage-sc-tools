namespace ScTools.LanguageServer.Services;

using System.IO;

public interface IServerIOProvider : IDisposable
{
    public Stream Sender { get; }
    public Stream Receiver { get; }
}

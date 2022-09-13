
namespace MDrude.Networking.Common;

public class TCPServerConnection {

    public string UID { get; set; }

    public Socket Socket { get; set; }

    public Stream Stream { get; set; }

    public IServerInterface Server { get; set; }

    public Task ListenTask { get; set; }

    public CancellationTokenSource ListenToken { get; set; }

    public bool Disconnected { get; set; }

    public async Task Write<T>(string uid, T ob) {

        await Server.Write(this, uid, ob);

    }

    public async Task Write(TCPFrame<TCPServerConnection> frame) {

        await frame.Write(Stream);

    }

}

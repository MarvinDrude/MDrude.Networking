
namespace MDrude.Networking.WebSockets;

public class WSClient : TCPClientInterface<WSClientOptions, WSServerConnection, WSHandshaker, WSFrame, TCPJsonSerializer> {

    public WSClient(string address, ushort port, WSClientOptions options) : base(address, port, options) {

    }

}

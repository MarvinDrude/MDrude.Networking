
namespace MDrude.Networking.WebSockets;

public class WSServer : TCPServerInterface<WSServerOptions, WSServerConnection, WSHandshaker, WSFrame, TCPJsonSerializer> {

    public WSServer(string address, ushort port, WSServerOptions options) : base(address, port, options) {



    }

}

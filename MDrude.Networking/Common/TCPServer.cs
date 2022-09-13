

namespace MDrude.Networking.Common;

public class TCPServer : TCPServerInterface<TCPServerOptions, TCPServerConnection, TCPHandshakerDefault, TCPFrameDefault, TCPJsonSerializer> {

    public TCPServer(string address, ushort port, TCPServerOptions options) : base(address, port, options) {

    }

}

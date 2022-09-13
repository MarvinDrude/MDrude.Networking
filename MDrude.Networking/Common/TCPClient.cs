
namespace MDrude.Networking.Common;

public class TCPClient : TCPClientInterface<TCPClientOptions, TCPServerConnection, TCPHandshakerDefault, TCPFrameDefault, TCPJsonSerializer> {

    public TCPClient(string address, ushort port, TCPClientOptions options) : base(address, port, options) {

    }

}

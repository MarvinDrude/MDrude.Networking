

namespace MDrude.Networking.Common;

public class TCPHandshaker<ServerConnection>
    where ServerConnection : TCPServerConnection {

    public virtual Task<bool> Handshake(ServerConnection conn) {

        throw new NotImplementedException("Not implemented");

    }

    public virtual Task<bool> ClientHandshake(Stream stream) {

        throw new NotImplementedException("Not implemented");

    }

}


public class TCPHandshakerDefault : TCPHandshaker<TCPServerConnection> {

    public override async Task<bool> Handshake(TCPServerConnection conn) {

        return true;

    }

    public override async Task<bool> ClientHandshake(Stream stream) {

        return true;

    }

}
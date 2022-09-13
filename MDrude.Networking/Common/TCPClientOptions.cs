
namespace MDrude.Networking.Common;

public class TCPClientOptions {

    public bool SslEnabled { get; set; } = false;

    public int ReconnectInterval { get; set; } = 3000;

    public string Host { get; set; }

}

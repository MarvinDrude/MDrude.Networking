
namespace MDrude.Networking.Common;

public class TCPServerOptions {

    public int Backlog { get; set; } = 500;

    public X509Certificate2 Certificate { get; set; } = null;

    public bool SslEnabled { get; set; } = false;

}

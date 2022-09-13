
namespace MDrude.Networking.Common;

public class TCPServerOptions {

    public int Backlog { get; set; } = 500;

    public X509Certificate2 Certificate { get; set; } = null;

    public bool SslEnabled { get; set; } = false;

    public bool RttEnabled { get; set; } = true;

    public int RttInterval { get; set; } = 45000;

}

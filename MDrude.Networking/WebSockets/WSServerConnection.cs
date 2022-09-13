
namespace MDrude.Networking.WebSockets;

public class WSServerConnection : TCPServerConnection {

    public WSMeta Meta { get; private set; } = new WSMeta();

}

public class WSMeta {

    public string UserAgent { get; set; } = "None";

    public string Cookies { get; set; }

    public string SetCookies { get; set; }

    public string IP { get; set; }

}
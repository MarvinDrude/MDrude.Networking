
using System.Reflection;
using System.Reflection.Metadata;

namespace MDrude.Networking.Common;

public interface IServerInterface {

    public Task Write<T>(TCPServerConnection conn, string uid, T ob);

    public Task Write(TCPServerConnection conn, string uid, Memory<byte> data);

}

public class TCPServerInterface<ServerOptions, ServerConnection, Handshaker, Frame, Serializer> : IServerInterface
    where ServerOptions : TCPServerOptions
    where ServerConnection : TCPServerConnection, new()
    where Handshaker : TCPHandshaker<ServerConnection>, new()
    where Frame : TCPFrame<ServerConnection>, new()
    where Serializer : TCPSerializer, new() {

    public delegate void DisconnectionHandler(ServerConnection connection, TCPDisconnection reason);
    public event DisconnectionHandler OnDisconnect;

    public delegate void ConnectHandler(ServerConnection connection, DateTime time);
    public event ConnectHandler OnConnect;

    public delegate void FrameHandler(ServerConnection connection, Frame frame);
    public event FrameHandler OnMessage;

    public ConcurrentDictionary<string, ServerConnection> Connections { get; private set; }

    public ServerOptions Options { get; private set; }

    public IPAddress Address { get; private set; }

    public ushort Port { get; private set; }

    public bool Running { get; private set; }

    public Serializer Serializing { get; set; }

    private Task ConnectionTask { get; set; }

    private CancellationTokenSource ConnectionToken { get; set; }

    private Socket Socket { get; set; }

    private Handshaker Handshaking { get; set; }

    private ConcurrentDictionary<string, TCPServerEventEmitter<ServerConnection>> Events { get; set; }

    private Task RttTask { get; set; }

    public TCPServerInterface(string address, ushort port, ServerOptions options) {

        if(!IPAddress.TryParse(address, out var adr)) {
            throw new TCPSocketException("Server address is not an valid IP.");
        }

        if(options.SslEnabled && options.Certificate == null) {
            throw new TCPSocketException("Server option ssl is enabled but no certificate specified.");
        }

        Address = adr;
        Options = options;
        Port = port;

        Connections = new ConcurrentDictionary<string, ServerConnection>();
        Events = new ConcurrentDictionary<string, TCPServerEventEmitter<ServerConnection>>();
        Running = false;

        Handshaking = new Handshaker();
        Serializing = new Serializer();

        On<Memory<byte>>("__inner-pong", async (buffer, conn) => {

            if (conn.RTT.Sending) {

                conn.RTT.Sending = false;

                var span = DateTime.UtcNow - conn.RTT.Sent;
                var ms = conn.RTT.Last = span.TotalMilliseconds;

                if (ms < conn.RTT.Min || conn.RTT.Min == -1) {
                    conn.RTT.Min = ms;
                }

                if (ms > conn.RTT.Max) {
                    conn.RTT.Max = ms;
                }

            }

        });

    }

    public bool Start() {

        if(Running) {
            return false;
        }

        Connections.Clear();
        Running = true;

        ConnectionToken = new CancellationTokenSource();

        ConnectionTask = new Task(async () => { await ListenConnections(); }, ConnectionToken.Token, TaskCreationOptions.LongRunning);
        ConnectionTask.Start();

        if(Options.RttEnabled) {

            RttTask = new Task(async () => { await RoutineRtt(); }, ConnectionToken.Token, TaskCreationOptions.LongRunning);
            RttTask.Start();

        }

        Logger.DebugWrite("INFO", $"Server started.");

        return true;

    }

    public bool Stop() {

        if(!Running) {
            return false;
        }

        ConnectionToken?.Cancel();
        
        foreach(var key in Connections.Keys) {

            if(Connections.TryGetValue(key, out var conn)) {

                RemoveClient(conn, TCPDisconnection.ServerShutdown);
                conn.ListenToken?.Cancel();

            }

        }

        try {

            Socket.Shutdown(SocketShutdown.Both);

        } catch(Exception) {

        }

        Socket?.Close();
        Running = false;

        Logger.DebugWrite("INFO", $"Server stopped.");

        return true;

    }

    public void RemoveClient(ServerConnection conn, TCPDisconnection reason) {

        bool shootEvent = false;

        try {

            if (conn == null) {
                return;
            }

            conn.Disconnected = true;

            ServerConnection outer;

            if (Connections.ContainsKey(conn.UID)) {
                Connections.TryRemove(conn.UID, out outer);
                shootEvent = true;
            }

            if (conn.ListenToken != null)
                conn.ListenToken.Cancel();

            if (conn.Socket != null)
                conn.Socket.Shutdown(SocketShutdown.Both);

        } catch (Exception) { }

        if(shootEvent) {

            Logger.DebugWrite("INFO", $"Socket got removed. UID: {conn.UID}, Reason: {reason}");
            OnDisconnect?.Invoke(conn, reason);

        }

    }

    public async Task Write(TCPServerConnection conn, string uid, Memory<byte> data) {

        await Write(conn, new Frame() {
            ID = uid,
            Data = data
        });

    }

    public async Task Write<T>(TCPServerConnection conn, string uid, T ob) {

        await Write(conn, new Frame() {
            ID = uid,
            Data = Serializing.Serialize(ob)
        });

    }

    public async Task Write(TCPServerConnection conn, Frame frame) {

        await Write((ServerConnection)conn, frame);

    }

    public async Task Write<T>(ServerConnection conn, string uid, T ob) {

        await Write(conn, new Frame() {
            ID = uid,
            Data = Serializing.Serialize(ob)
        });

    }

    public async Task Write(ServerConnection conn, Frame frame) {

        await frame.Write(conn.Stream);

    }

    public async Task Broadcast<T>(string uid, T ob) {

        await Broadcast(new Frame() {
            ID = uid,
            Data = Serializing.Serialize(ob)
        });

    }

    public async Task Broadcast(Frame frame) {

        foreach(var keypair in Connections) {

            await frame.Write(keypair.Value.Stream);

        }

    }

    public void On<T>(string uid, Func<T, ServerConnection, Task> listener) {

        if (!Events.TryGetValue(uid, out TCPServerEventEmitter<ServerConnection> entry)) {
            entry = new TCPServerEventEmitter<ServerConnection>(uid);
            Events[uid] = entry;
        }

        entry.AddListener(listener);

    }

    public bool RemoveListener<T>(string uid, Func<T, ServerConnection, Task> listener) {

        if (Events.TryGetValue(uid, out TCPServerEventEmitter<ServerConnection> entry)) {

            bool removed = entry.RemoveListener(listener);

            if (entry.Listeners.Count == 0) {
                Events.TryRemove(uid, out var garbage);
            }

            return removed;

        }

        return false;

    }

    private async Task Emit(string uid, ServerConnection conn, Frame message) {

        if (Events.TryGetValue(uid, out TCPServerEventEmitter<ServerConnection> entry)) {

            foreach(var ob in entry.Listeners) {

                if(ob.Type == typeof(Memory<byte>)) {
                    await ob.Function(message.Data, conn);
                } else {
                    dynamic target = Serializing.Deserialize(message.Data, ob.Type);
                    await ob.Function(target, conn);
                }

            }

        }

    }

    private async Task RoutineRtt() {

        while(Running && !ConnectionToken.IsCancellationRequested) {

            try {

                await Task.Delay(Options.RttInterval, ConnectionToken.Token);

            } catch(Exception) { }

            if(!ConnectionToken.IsCancellationRequested) {

                foreach(var keypair in Connections) {

                    var conn = keypair.Value;

                    if(conn.Socket == null || conn.Disconnected) {
                        continue;
                    }

                    if(conn.RTT.Sending) {

                        conn.RTT.Last = Options.RttInterval;

                        if (conn.RTT.Max < Options.RttInterval) {
                            conn.RTT.Max = Options.RttInterval;
                        }

                    }

                    conn.RTT.Sending = true;
                    conn.RTT.Sent = DateTime.UtcNow;

                    Memory<byte> payload = TCPReaderWriter.WriteFloat((float)conn.RTT.Last);
                    await conn.Write("__inner-ping", payload);

                }

            }

        }

    }

    private async Task ListenConnections() {

        Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

        Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        Socket.Bind(new IPEndPoint(Address, Port));

        Socket.Listen(Options.Backlog);

        while(Running && !ConnectionToken.IsCancellationRequested) {

            Socket socket = null;

            try {

                Logger.DebugWrite("INFO", $"Wait for new connections.");
                socket = await Socket.AcceptAsync(ConnectionToken.Token);

            } catch(Exception er) {

                Logger.DebugWrite("FAILED", $"Socket.AcceptAsync error occured: {er.Message}");
                continue;

            }

            ServerConnection conn = new ServerConnection() {
                Socket = socket,
                UID = RandomGen.GenRandomUID(Connections, 48),
                Server = this
            };

            while(!Connections.TryAdd(conn.UID, conn)) {
                conn.UID = RandomGen.GenRandomUID(Connections, 48);
            }

            CancellationTokenSource cancel = new CancellationTokenSource();

            Task listenTask = new Task(async () => {

                try {

                    await ListenClient(conn);

                } catch(Exception er) {

                    Logger.DebugWrite("FAILED", $"Connection listen task error: {er.Message}");
                    RemoveClient(conn, TCPDisconnection.Disconnect);

                }

            }, cancel.Token, TaskCreationOptions.LongRunning);

            conn.ListenTask = listenTask;
            conn.ListenToken = cancel;

            listenTask.Start();

        }

    }

    private async Task ListenClient(ServerConnection conn) {

        conn.Stream = await GetStream(conn.Socket);

        if(conn.Stream == null) {

            Logger.DebugWrite("FAILED", $"User stream null");
            RemoveClient(conn, TCPDisconnection.Disconnect);

            return;

        }

        using Stream ns = conn.Stream;
        bool res = await Handshaking.Handshake(conn);

        if(!res) {
            RemoveClient(conn, TCPDisconnection.WrongHeader);
            return;
        }

        OnConnect?.Invoke(conn, DateTime.Now);

        while(Running && !conn.ListenToken.IsCancellationRequested) {

            Frame frame = new Frame();
            
            if(!(await frame.Read(conn))) {

                RemoveClient(conn, TCPDisconnection.Disconnect);
                break;

            }

            if(frame.ID == null) {
                continue;
            }

            OnMessage?.Invoke(conn, frame);
            await Emit(frame.ID, conn, frame);

        }

    }

    private async Task<Stream> GetStream(Socket socket) {

        Stream stream = new NetworkStream(socket);

        if (!Options.SslEnabled) {

            return stream;

        }

        SslStream sslStream = null;

        try {

            sslStream = new SslStream(stream, false);
            var task = sslStream.AuthenticateAsServerAsync(Options.Certificate, false, SslProtocols.None, true);
            await task.WaitAsync(TimeSpan.FromSeconds(60));

            return sslStream;

        } catch (Exception er) {

            if (sslStream != null) {
                await sslStream.DisposeAsync();
            }

            if (er is TimeoutException) {
                Logger.DebugWrite("FAILED", $"Ssl authenitcation timeout.");
            }

            Logger.DebugWrite("FAILED", $"Certification fail get stream: {er}");
            return null;

        }

    }

}
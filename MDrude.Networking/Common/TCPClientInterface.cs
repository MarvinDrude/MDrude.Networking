
namespace MDrude.Networking.Common;

public class TCPClientInterface<ClientOptions, ServerConnection, Handshaker, Frame, Serializer>
    where ClientOptions : TCPClientOptions
    where ServerConnection : TCPServerConnection, new()
    where Handshaker : TCPHandshaker<ServerConnection>, new()
    where Frame : TCPFrame<ServerConnection>, new()
    where Serializer : TCPSerializer, new() {

    public delegate void ConnectHandler();
    public event ConnectHandler OnConnect;

    public delegate void DisconnectHandler(TCPDisconnection reason);
    public event DisconnectHandler OnDisconnect;

    public delegate void FrameHandler(Frame frame);
    public event FrameHandler OnMessage;

    public ClientOptions Options { get; private set; }

    public Socket Socket { get; private set; }

    public bool Running { get; private set; }

    public TCPClientInterface<ClientOptions, ServerConnection, Handshaker, Frame, Serializer> Writer => this;

    public IPAddress Address { get; private set; }

    public ushort Port { get; private set; }

    private Handshaker Handshaking { get; set; }

    private Task ListenTask { get; set; }

    private CancellationTokenSource ListenToken { get; set; }

    private Task ConnectTask { get; set; }

    private CancellationTokenSource ConnectToken { get; set; }

    private Stream Stream { get; set; }

    private Serializer Serializing { get; set; }

    private ConcurrentDictionary<string, TCPClientEventEmitter> Events { get; set; }

    public TCPClientInterface(string address, ushort port, ClientOptions options) {

        if (!IPAddress.TryParse(address, out var adr)) {
            throw new TCPSocketException("Client address is not an valid IP.");
        }

        Address = adr;
        Options = options;
        Port = port;

        Running = false;
        Handshaking = new Handshaker();
        Serializing = new Serializer();

        Events = new ConcurrentDictionary<string, TCPClientEventEmitter>();

        On<Memory<byte>>("__inner-ping", async (buffer) => {
            await Write("__inner-pong", buffer);
        });

    }

    public void Connect() {

        Running = true;

        ConnectToken = new CancellationTokenSource();
        ConnectTask = new Task(async () => { await Connecting(); }, ConnectToken.Token, TaskCreationOptions.LongRunning);

        ConnectTask.Start();

    }

    public void Disconnect() {

        Running = false;

        ConnectToken?.Cancel();
        ListenToken?.Cancel();

        try {

            Socket.Shutdown(SocketShutdown.Both);

        } catch (Exception) {

        }

        Socket?.Close();

    }

    public async Task Send(string uid, Memory<byte> data) {

        await Write(uid, data);

    }

    public async Task Write(string uid, Memory<byte> data) {

        await Write(new Frame() {
            ID = uid,
            Data = data
        });

    }

    public async Task Send<T>(string uid, T ob) {

        await Write(uid, ob);

    }

    public async Task Write<T>(string uid, T ob) {

        await Write(new Frame() {
            ID = uid,
            Data = Serializing.Serialize(ob)
        });

    }

    public async Task Write(Frame message) {

        await message.Write(Stream);

    }

    public async Task WriteFaulty(Frame message) {

        await message.WriteFaulty(Stream);

    }

    public void On<T>(string uid, Func<T, Task> listener) {

        if (!Events.TryGetValue(uid, out TCPClientEventEmitter entry)) {
            entry = new TCPClientEventEmitter(uid);
            Events[uid] = entry;
        }

        entry.AddListener(listener);

    }

    public bool RemoveListener<T>(string uid, Func<T, Task> listener) {

        if (Events.TryGetValue(uid, out TCPClientEventEmitter entry)) {

            bool removed = entry.RemoveListener(listener);

            if (entry.Listeners.Count == 0) {
                Events.TryRemove(uid, out var garbage);
            }

            return removed;

        }

        return false;

    }

    private async Task Emit(string uid, Frame message) {

        if (Events.TryGetValue(uid, out TCPClientEventEmitter entry)) {

            foreach (var ob in entry.Listeners) {

                if(ob.Type == typeof(Memory<byte>)) {
                    await ob.Function(message.Data);
                } else {
                    dynamic target = Serializing.Deserialize(message.Data, ob.Type);
                    await ob.Function(target);
                }

            }

        }

    }

    private void Remove(TCPDisconnection reason) {

        Disconnect();

        Logger.DebugWrite("INFO", $"Client Disconnection Reason: {reason}");
        OnDisconnect?.Invoke(reason);

    }

    private async Task Connecting() {

        while(Running && !ConnectToken.IsCancellationRequested) {

            try {

                Logger.Write("INFO", $"Try connecting to {Address}:{Port}");

                Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                Socket.NoDelay = true;

                Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

                await Socket.ConnectAsync(new IPEndPoint(Address, Port));

                Stream = await GetStream(Socket);
                bool res = await Handshaking.ClientHandshake(Stream);

                if(res) {

                    ListenToken = new CancellationTokenSource();
                    ListenTask = new Task(async () => { await Listen(); }, ListenToken.Token, TaskCreationOptions.LongRunning);

                    ListenTask.Start();

                } else {

                    throw new TCPSocketException("Invalid server response");

                }

                ConnectToken.Cancel();
                return;

            } catch(Exception) {

                Logger.DebugWrite("INFO", $"Client couldn't connect to {Address}:{Port}");

            }

            await Task.Delay(Options.ReconnectInterval);

        }

    }

    private async Task Listen() {

        using Stream ns = Stream;

        Logger.Write("INFO", "Successfully connected.");
        OnConnect?.Invoke();

        while(Running && !ListenToken.IsCancellationRequested) {

            Frame frame = new Frame();

            if (!(await frame.Read(ns))) {

                Remove(TCPDisconnection.Disconnect);
                break;

            }

            if(frame.ID == null) {
                continue;
            }

            OnMessage?.Invoke(frame);
            await Emit(frame.ID, frame);

        }

    }

    private async Task<Stream> GetStream(Socket socket) {

        Stream stream = new NetworkStream(socket);

        if (!Options.SslEnabled) {

            return stream;

        }

        try {

            SslStream sslStream = new SslStream(stream, false);
            await sslStream.AuthenticateAsClientAsync(Options.Host ?? Address.ToString());

            return sslStream;

        } catch (Exception) {

            return stream;

        }

    }

}

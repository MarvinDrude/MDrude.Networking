
using System.Threading.Channels;

namespace MDrude.Networking.Common;

public class TCPServerConnection {

    public string UID { get; set; }

    public Socket Socket { get; set; }

    public Stream Stream { get; set; }

    public IServerInterface Server { get; set; }

    public Task ListenTask { get; set; }

    public CancellationTokenSource ListenToken { get; set; }

    private bool _Diconnected;
    public bool Disconnected { 
        get {
            return _Diconnected;
        } 
        set {
            _Diconnected = value;
            if(_Diconnected && !Source.IsCancellationRequested) {
                Source.Cancel();
            }
        }
    }

    public RTT RTT { get; set; } = new RTT();

    public TCPServerConnection Writer => this;

    private Channel<Func<Task>> Messages { get; set; }

    private CancellationTokenSource Source { get; set; }

    public TCPServerConnection() {

        Source = new CancellationTokenSource();
        Messages = Channel.CreateUnbounded<Func<Task>>();

        new Task(async () => {

            try {

                while(!Disconnected) {

                    var func = await Messages.Reader.ReadAsync(Source.Token);

                    try {

                        await func();

                    } catch(Exception) {

                    }

                }

            } catch(Exception) {

            }

        }, TaskCreationOptions.LongRunning).Start();

    }

    public async Task Send(string uid, Memory<byte> data) {

        if (Disconnected) return;

        await Messages.Writer.WriteAsync(async () => {
            await Write(uid, data);
        });

    }

    public async Task Write(string uid, Memory<byte> data) {

        if (Disconnected) return;

        await Messages.Writer.WriteAsync(async () => {
            await Server.Write(this, uid, data);

        });


    }

    public async Task Send<T>(string uid, T ob) {

        if (Disconnected) return;

        await Messages.Writer.WriteAsync(async () => {
            await Write(uid, ob);

        });


    }

    public async Task Write<T>(string uid, T ob) {

        if (Disconnected) return;

        await Messages.Writer.WriteAsync(async () => {

            await Server.Write(this, uid, ob);

        });

    }

    public async Task Write(TCPFrame<TCPServerConnection> frame) {

        if (Disconnected) return;

        await Messages.Writer.WriteAsync(async () => {
            await frame.Write(Stream);

        });


    }

}

public class RTT {

    public bool Sending { get; set; } = false;

    public DateTime Sent { get; set; }

    public double Last { get; set; }

    public double Max { get; set; }

    public double Min { get; set; } = -1d;

}

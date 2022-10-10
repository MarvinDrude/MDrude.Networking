# Networking Library for TCP/WS Server / Client - lightweight
This is a library to have a very lightweight C# TCP Server / Client in .NET 6 and going forward. It also comes with a already working WebSocket Server and Client (WebSocket.js) that conforms with RFC 6455 which ensures compatibility. It can send binary data as well as arbitrary objects by serializing them into JSON.
Built with newest C# features like Memory<T> and also runs completely async to ensure max performance. It is used in bigger projects with multiple hundreds of concurrent users without any problems.

## Features
- Easy to expand on
- Light and blazingly fast :)
- Message Events Interface is socket.io like easy to use
- WebSocket Support out of the box
- SSL Support
- Async
- Object serialization
- Raw byte data

## How to use as TCP
If you want to use the TCP Server / Client out of the box, you will need to use class TCPServer and TCPClient.
Following is an easy example of how to use it
```C#
TCPServer server = new TCPServer("127.0.0.1", 27789, new TCPServerOptions() {
    // Whether ssl is enabled or not
    SslEnabled = false,
    // Certificate if ssl is enabled
    Certificate = null,
    // Whether periodically there are pings sent to calculate RTT for the clients
    RttEnabled = false,
    // Defaults to 500 and is used for what the socket uses as backlog
    Backlog = 500,
    // Interval of RTT pings are sent to all connected clients
    RttInterval = 45000
});

server.On<Memory<byte>>("test-message", async (mess, conn) => {

    // mess contains the raw bytes received without header bytes
    Logger.Write("INFO", $"First two bytes received in content: {mess.Span[0]} {mess.Span[1]}");
    await Task.Delay(3000);

    // sending raw bytes to client
    await conn.Send("test-message", mess);

});

server.On<TestDataMessage>("test-json", async (mess, conn) => {

    // mess contains object created by json
    Logger.Write("INFO", $"JSON object received: {mess.Name}: {mess.Data} {mess.Number}");
    await Task.Delay(5000);

    mess.Number++;
    // sending object as json to client
    await conn.Send("test-json", mess);

});

server.OnConnect += async (conn, time) => {

    // sending object as json to client on connect
    await conn.Send("test-json", new TestDataMessage() {
        Name = "NameTest",
        Data = "DataTest",
        Number = 0
    });
    // sending raw bytes to client on connect
    await conn.Send("test-message", new Memory<byte>(new byte[] {
        12, 13, 14
    }));

};

server.Start();

TCPClient client = new TCPClient("127.0.0.1", 27789, new TCPClientOptions() {
    // Host needed if ssl enabled www.google.com for example
    Host = null,
    // Time between connect tries in ms
    ReconnectInterval = 2000,
    // Whether ssl is enabled or not
    SslEnabled = false
});

client.On<Memory<byte>>("test-message", async (mess) => {

    // send raw bytes to server
    await client.Send("test-message", mess);

});

client.On<TestDataMessage>("test-json", async (mess) => {

    // send json object to server
    await client.Send("test-json", mess);

});

client.Connect();
```
### Customizing TCP Serialization / Handshake / Frame
The out of the box TCPServer/TCPClient looks like this:
```C#
public class TCPServer 
    : TCPServerInterface<TCPServerOptions, TCPServerConnection, TCPHandshakerDefault, TCPFrameDefault, TCPJsonSerializer> {

    public TCPServer(string address, ushort port, TCPServerOptions options) : base(address, port, options) {

    }

}

public class TCPClient 
    : TCPClientInterface<TCPClientOptions, TCPServerConnection, TCPHandshakerDefault, TCPFrameDefault, TCPJsonSerializer> {

    public TCPClient(string address, ushort port, TCPClientOptions options) : base(address, port, options) {

    }

}
```
In order to customize how the handshake / frame / serialization is done you have to just implement your own version of those and put those in the generics.
The default serialization looks very simple and you can create your own in the same fashion:
```C#
public class TCPJsonSerializer : TCPSerializer {

    public override T Deserialize<T>(Memory<byte> buffer) {

        string text = Encoding.UTF8.GetString(buffer.Span);

        try {

            return JsonConvert.DeserializeObject<T>(text);

        } catch(Exception) {

            return default;

        }

    }

    public override object Deserialize(Memory<byte> buffer, Type type) {

        string text = Encoding.UTF8.GetString(buffer.Span);

        try {

            return JsonConvert.DeserializeObject(text, type);

        } catch (Exception) {

            return default;

        }

    }

    public override Memory<byte> Serialize<T>(T ob) {

        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ob));

    }

}
```
## How to use it for WebSockets out of the box
This code example shows a nearly identical example to the TCP example but for WebSockets:
```C#
WSServer server = new WSServer("127.0.0.1", 27789, new WSServerOptions() {
    // Whether ssl is enabled or not
    SslEnabled = false,
    // Certificate if ssl is enabled
    Certificate = null,
    // Whether periodically there are pings sent to calculate RTT for the clients
    RttEnabled = false,
    // Defaults to 500 and is used for what the socket uses as backlog
    Backlog = 500,
    // Interval of RTT pings are sent to all connected clients
    RttInterval = 45000
});

server.On<Memory<byte>>("test-message", async (mess, conn) => {

    // mess contains the raw bytes received without header bytes
    Logger.Write("INFO", $"First two bytes received in content: {mess.Span[0]} {mess.Span[1]}");
    await Task.Delay(10);

    // sending raw bytes to client
    await conn.Send("test-message", mess);

});

server.On<TestDataMessage>("test-json", async (mess, conn) => {

    // mess contains object created by json
    Logger.Write("INFO", $"JSON object received: {mess.Name}: {mess.Data} {mess.Number}");
    await Task.Delay(20);

    mess.Number++;
    // sending object as json to client
    await conn.Send("test-json", mess);

});

server.OnConnect += async (conn, time) => {

    // sending object as json to client on connect
    await conn.Send("test-json", new TestDataMessage() {
        Name = "NameTest",
        Data = "DataTest",
        Number = 0
    });
    // sending raw bytes to client on connect
    await conn.Send("test-message", new Memory<byte>(new byte[] {
        12, 13, 14
    }));

};

server.Start();

WSClient client = new WSClient("127.0.0.1", 27789, new WSClientOptions() {
    // Host needed if ssl enabled www.google.com for example
    Host = null,
    // Time between connect tries in ms
    ReconnectInterval = 2000,
    // Whether ssl is enabled or not
    SslEnabled = false
});

client.On<Memory<byte>>("test-message", async (mess) => {

    // send raw bytes to server
    await client.Send("test-message", mess);

});

client.On<TestDataMessage>("test-json", async (mess) => {

    // send json object to server
    await client.Send("test-json", mess);

});

client.Connect();
```
### Connect to the WS Server via JavaScript
I highly recommend using the custom JavaScript client (very small) to interact with this server due to the custom protocol on top of websocket in order to have all messages labeled with an uid of yours (The JavaScript code can be found under MDrude.Networking/WebSocket.js): 
```JS
let test = new MD.JsonWebSocket({
    "address": "ws://127.0.0.1:27789"
});

test.on("example-message", async (ob) => {
    console.log(ob);
});

test.on("connect", async () => {

    let sending = new Uint8Array(32);

    test.send("example-message", { "payload-example": "x" });
    test.sendBinary("binary-example", sending);

});

test.connect();
```








using MDrude.Networking.Common;
using MDrude.Networking.Utils;
using MDrude.Networking.WebSockets;
using System.Net;
using System.Net.Sockets;

namespace MDrude.NetworkingTest;

public class Examples {

    public static async Task ExampleOne() {

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

    }

    public static async Task ExampleTwo() {

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

    }

}

public class TestDataMessage {

    public string Data { get; set; }

    public string Name { get; set; }

    public int Number { get; set; }

}
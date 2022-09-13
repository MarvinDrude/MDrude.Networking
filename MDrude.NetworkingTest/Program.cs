
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using MDrude.NetworkingTest;
using MDrude.Networking.Common;
using MDrude.Networking.Utils;
using System.Reflection;

Logger.AddDefaultConsoleLogging();

TCPServer server = new TCPServer("127.0.0.1", 27789, new TCPServerOptions() {
    SslEnabled = false
});

server.OnMessage += async (conn, mess) => {

    Console.WriteLine($"New message arrived server: {mess.ID}, {mess.Data.Length} {mess.Data.Span[122]}");

};

server.Start();

TCPClient client = new TCPClient("127.0.0.1", 27789, new TCPClientOptions() {
    SslEnabled = false
});

client.OnConnect += async () => {

    byte[] data = new byte[200];
    RandomNumberGenerator.Fill(data);

    await client.Write(new TCPFrameDefault() {
        ID = "Test-Receiver",
        Data = data
    });

};

client.Connect();

Console.ReadLine();
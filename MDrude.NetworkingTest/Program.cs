
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using MDrude.NetworkingTest;
using MDrude.Networking.Common;
using MDrude.Networking.Utils;
using System.Reflection;
using System.Text;

Logger.AddDefaultConsoleLogging();

TCPServer server = new TCPServer("127.0.0.1", 27789, new TCPServerOptions() {
    SslEnabled = false
});


server.On<TestDataMessage>("Test-message", async (mess, conn) => {
    Console.WriteLine("Test-message: " + mess.Data + " " + mess.Name);
    await conn.Write("Test-message", new TestDataMessage() {
        Data = "Vom server vesendet",
        Name = "Wubblors"
    });
});

server.On<TestDataMessage>("Test-message1", async (mess, conn) => {
    Console.WriteLine("Test-message1: " + mess.Data + " " + mess.Name);
});

server.Start();

TCPClient client = new TCPClient("127.0.0.1", 27789, new TCPClientOptions() {
    SslEnabled = false
});

client.On<TestDataMessage>("Test-message", async (mess) => {
    Console.WriteLine("Test-message: " + mess.Data + " " + mess.Name);
});

client.OnConnect += async () => {

    await client.Write("Test-message", new TestDataMessage() {
        Data = "Marvin war hier",
        Name = "Wubblors"
    });

    await client.Write("Test-message1", new TestDataMessage() {
        Data = "vdsvdsvdsdsvds",
        Name = "adsadsa"
    });

};

client.Connect();

Console.ReadLine();
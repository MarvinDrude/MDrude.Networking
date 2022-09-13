
using MDrude.Networking.Common;
using MDrude.Networking.Utils;
using MDrude.Networking.WebSockets;
using MDrude.NetworkingTest;

Logger.AddDefaultConsoleLogging();

string adr = "46.4.114.216";
//adr = "127.0.0.1";

WSServer server = new WSServer(adr, 54342, new WSServerOptions() {
    RttEnabled = false,
    SslEnabled = false
});

server.OnConnect += async (conn, time) => {

    Logger.Write("INFO", "Connected clients: " + server.Connections.Count);

    await conn.Write("perf-message", new TestDataMessage() {
        Number = 2,
        Data = "Wusmaf ads ads ad sa dsa dsa das dsads dsa dsa ",
        Name = "sda DSA dsaD sa DAS dsa dsa DASDADSAD sadsadsadsa vf"
    });

};

server.On<TestDataMessage>("perf-message", async (mess, conn) => {

    mess.Data = mess.Data.Reverse().ToString();
    mess.Number += 50;

    mess.Number = (int)Math.Sqrt(mess.Number);

    mess.Name = mess.Name.ToLower();

    await conn.Write("perf-message", mess);

});

server.Start();

Console.ReadLine();